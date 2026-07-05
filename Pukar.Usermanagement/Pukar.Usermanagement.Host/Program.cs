using Microsoft.EntityFrameworkCore;
using Pukar.Usermanagement.API.Extensions;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Host.Bootstrap;
using Pukar.Usermanagement.Host.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddHostedService<AdminUserSeedHostedService>();
        builder.Services.AddHostedService<ServiceClientSeedHostedService>();
        builder.Services.AddHostedService<IdempotencyCleanupHostedService>();
    }

    var corsSection = builder.Configuration.GetSection(CorsOptions.SectionName);
    var corsOrigins = corsSection.Get<CorsOptions>()?.AllowedOrigins
        ?? corsSection.Get<string[]>()
        ?? Array.Empty<string>();

    if (corsOrigins.Length == 0 && builder.Environment.IsDevelopment())
    {
        corsOrigins =
        [
            "http://localhost:3000",
            "https://localhost:3000",
            "http://127.0.0.1:3000",
            "https://127.0.0.1:3000",
        ];
    }

    if (corsOrigins.Length == 0 && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    {
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must contain at least one origin outside Development.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("UmWeb", policy =>
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
    });

    var useRsaSigning = !builder.Environment.IsEnvironment("Testing");

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddPukarUserManagementCore(
        builder.Configuration,
        "UserManagementDb",
        useRsaSigning: useRsaSigning,
        registerDbContext: !builder.Environment.IsEnvironment("Testing"));
    builder.Services.AddPukarUserManagementAuthentication(builder.Configuration, useRsaSigning: useRsaSigning);

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<UserManagementDbContext>(name: "database");

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        db.Database.Migrate();
        Log.Information("Applied pending EF Core migrations for UserManagementDbContext (Development).");
        app.MapOpenApi().AllowAnonymous();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("UmWeb");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapControllers();

    Log.Information("Pukar.Usermanagement.Host starting ({Environment})", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Pukar.Usermanagement.Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
