using EMS.API.Bootstrap;
using EMS.API.Middleware;
using EMS.API.Options;
using EMS.API.Services;
using EMS.Application.Services.Departments;
using EMS.Application.Services.Employees;
using EMS.Application.Services.JobPositions;
using EMS.Application.Services.Locations;
using EMS.Application.Services.Organizations;
using EMS.Domain.Database;
using EMS.Domain.Repositories.Interface;
using EMS.Infrastructure.Repositories.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Pukar.Usermanagement.API.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);

        var mongoLogs = context.Configuration.GetConnectionString("MongoLogs");
        if (!string.IsNullOrWhiteSpace(mongoLogs))
        {
            // v7 sink: database from URL path (ems-logs); default collection name is "log"
            configuration.WriteTo.MongoDBBson(mongoLogs);
        }
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("EmsWeb", policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "https://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
    builder.Services.AddHostedService<AdminUserSeedHostedService>();

    builder.Services.AddControllers(options =>
        {
            options.Filters.Add(
                new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        })
        .AddPukarUserManagementControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddPukarUserManagementApi(builder.Configuration);

    builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IOrganizationService, OrganizationService>();
    builder.Services.AddScoped<IDepartmentService, DepartmentService>();
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IJobPositionService, JobPositionService>();
    builder.Services.AddScoped<LocalOrganizationLogoStorage>();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name!)));

    var mongoLogsCs = builder.Configuration.GetConnectionString("MongoLogs");
    var healthChecks = builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>(name: "database");

    if (!string.IsNullOrWhiteSpace(mongoLogsCs))
    {
        builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoLogsCs));
        var mongoDbName = MongoUrl.Create(mongoLogsCs).DatabaseName ?? "ems-logs";
        healthChecks.AddMongoDb(
            clientFactory: sp => sp.GetRequiredService<IMongoClient>(),
            databaseNameFactory: _ => mongoDbName,
            name: "mongodb");
    }

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            if (httpContext.Items.TryGetValue("CorrelationId", out var cid) && cid is string s)
                diagnosticContext.Set("CorrelationId", s);
        };
    });

    app.UseHttpsRedirection();

    var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "organizations"));

    app.UseStaticFiles();

    app.UseCors("EmsWeb");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapControllers();

    Log.Information("EMS.API starting ({Environment})", app.Environment.EnvironmentName);

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
