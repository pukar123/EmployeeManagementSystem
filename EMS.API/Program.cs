using EMS.API.Bootstrap;
using EMS.API.Middleware;
using EMS.API.Options;
using EMS.API.Services;
using System.Security.Claims;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Departments;
using EMS.Application.Services.Documents;
using EMS.Application.Services.Employees;
using EMS.Application.Services.EmployeeSites;
using EMS.Application.Services.Attendance;
using EMS.Application.Services.JobPositions;
using EMS.Application.Services.Locations;
using EMS.Application.Services.Menus;
using EMS.Application.Services.Navigation;
using EMS.Application.Services.Organizations;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Sites;
using EMS.Application.Services.Tasks;
using EMS.Domain.Database;
using EMS.Domain.Repositories.Interface;
using Pukar.Usermanagement.Domain.Database;
using EMS.Infrastructure.Repositories.Implementations;
using EMS.Infrastructure.Integrations.UserManagement;
using EMS.Infrastructure.Persistence.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Pukar.Usermanagement.API.Extensions;
using Pukar.Usermanagement.Application;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // #region agent log
    static void DebugLog(string runId, string hypothesisId, string location, string message, object data)
    {
        var payload = JsonSerializer.Serialize(new
        {
            sessionId = "e16419",
            runId,
            hypothesisId,
            location,
            message,
            data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        File.AppendAllText("debug-e16419.log", payload + Environment.NewLine);
    }
    // #endregion

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
                .WithOrigins(
                    "http://localhost:3000",
                    "https://localhost:3000",
                    "http://127.0.0.1:3000",
                    "https://127.0.0.1:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
    builder.Services.Configure<AuthorizationModeOptions>(builder.Configuration.GetSection(AuthorizationModeOptions.SectionName));
    builder.Services.Configure<UserManagementApiOptions>(builder.Configuration.GetSection(UserManagementApiOptions.SectionName));
    builder.Services.AddHostedService<AdminUserSeedHostedService>();
    builder.Services.AddHostedService<EmsRbacSeedHostedService>();

    builder.Services.AddControllers(options =>
        {
            options.Filters.Add(
                new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        })
        .AddPukarUserManagementControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddPukarUserManagementApi(builder.Configuration);
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminAccess", policy =>
            policy.RequireAssertion(context =>
                context.User.Claims.Any(c =>
                    c.Type == ClaimTypes.Role
                    && string.Equals(c.Value, WellKnownRoles.Admin, StringComparison.OrdinalIgnoreCase))
                || context.User.Claims.Any(c =>
                    c.Type == "roles"
                    && string.Equals(c.Value, WellKnownRoles.AdminNormalizedName, StringComparison.OrdinalIgnoreCase))));
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
    builder.Services.AddSingleton<AuthorizationTelemetry>();
    builder.Services.AddSingleton<IAuthorizationTelemetry>(sp => sp.GetRequiredService<AuthorizationTelemetry>());
    builder.Services.AddSingleton<IAuthorizationTelemetryReporter>(sp => sp.GetRequiredService<AuthorizationTelemetry>());
    builder.Services.AddScoped<IAuthorizationCutoverReadinessReporter, AuthorizationCutoverReadinessReporter>();
    builder.Services.AddScoped<IIdentityContext, HttpContextIdentityContext>();
    builder.Services.AddScoped<IAuditContextAccessor, HttpContextAuditContextAccessor>();
    builder.Services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
    builder.Services.AddScoped<IRoleKeyPermissionService, RoleKeyPermissionService>();
    builder.Services.AddHttpClient<IUserManagementRoleMetadataClient, UserManagementRoleMetadataClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<UserManagementApiOptions>>().Value;
        if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl, options.RolesMetadataPath);
        }

        var timeout = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 5;
        client.Timeout = TimeSpan.FromSeconds(timeout);
    });
    builder.Services.AddScoped<INavigationService, NavigationService>();
    builder.Services.AddScoped<IMenuService, MenuService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IEmployeeRoleSyncService, EmployeeRoleSyncService>();
    builder.Services.AddScoped<IEmployeeRoleService, EmployeeRoleService>();
    builder.Services.AddScoped<IEmployeeUserManagementGateway, EmployeeUserManagementGateway>();
    builder.Services.AddScoped<IEmployeeIdentityProvisioningService, EmployeeIdentityProvisioningService>();
    builder.Services.AddScoped<IOrganizationService, OrganizationService>();
    builder.Services.AddScoped<IDepartmentService, DepartmentService>();
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IJobPositionService, JobPositionService>();
    builder.Services.AddScoped<IPositionRoleService, PositionRoleService>();
    builder.Services.AddScoped<IDocumentService, DocumentService>();
    builder.Services.AddScoped<ISiteService, SiteService>();
    builder.Services.AddScoped<IEmployeeSiteService, EmployeeSiteService>();
    builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
    builder.Services.AddScoped<IAttendanceBreakRepository, AttendanceBreakRepository>();
    builder.Services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
    builder.Services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
    builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
    builder.Services.AddScoped<ILeaveRequestAttachmentRepository, LeaveRequestAttachmentRepository>();
    builder.Services.AddScoped<ILeavePolicyRuleRepository, LeavePolicyRuleRepository>();
    builder.Services.AddScoped<IAttendanceService, AttendanceService>();
    builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
    builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
    builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
    builder.Services.AddScoped<ILeavePolicyRuleService, LeavePolicyRuleService>();
    builder.Services.AddScoped<ILeaveAttachmentService, LeaveAttachmentService>();
    builder.Services.AddScoped<ILeaveAccrualService, LeaveAccrualService>();
    builder.Services.AddScoped<ILeaveImportService, LeaveImportService>();
    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<LocalOrganizationLogoStorage>();
    builder.Services.AddScoped<LocalDocumentFileStorage>();
    builder.Services.AddScoped<LocalLeaveAttachmentStorage>();
    builder.Services.AddScoped<AuditSaveChangesInterceptor>();

    builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        options
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>())
            .UseSqlServer(
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
        using (var scope = app.Services.CreateScope())
        {
            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManagementDb = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
            appDb.Database.Migrate();
            userManagementDb.Database.Migrate();
        }

        Log.Information("Applied pending EF Core migrations for AppDbContext and UserManagementDbContext (Development).");

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
    Directory.CreateDirectory(Path.Combine(webRootPath, "attachments", "Organization"));
    Directory.CreateDirectory(Path.Combine(webRootPath, "attachments", "Employee"));
    Directory.CreateDirectory(Path.Combine(webRootPath, "attachments", "Leave"));

    app.UseStaticFiles();

    app.UseCors("EmsWeb");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapControllers();

    Log.Information("EMS.API starting ({Environment})", app.Environment.EnvironmentName);
    // #region agent log
    DebugLog(
        "pre-fix",
        "H1_H2_H3",
        "EMS.API/Program.cs:startup",
        "API host about to run",
        new
        {
            pid = Environment.ProcessId,
            env = app.Environment.EnvironmentName,
            contentRoot = app.Environment.ContentRootPath,
            appBase = AppContext.BaseDirectory,
        });
    app.Lifetime.ApplicationStopping.Register(() =>
        DebugLog(
            "pre-fix",
            "H1_H2_H3",
            "EMS.API/Program.cs:stopping",
            "API host stopping",
            new { pid = Environment.ProcessId }));
    // #endregion

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
