using EMS.API.Auth;
using EMS.API.Bootstrap;
using EMS.API.Middleware;
using EMS.API.Options;
using EMS.API.Services;
using EMS.Application.Options;
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
using EMS.Application.Services.Shifts;
using EMS.Application.Services.EmployeePortal;
using EMS.Application.Services.Manager;
using EMS.Application.Services.Onboarding;
using EMS.Application.Services.Notifications;
using EMS.Domain.Database;
using EMS.Domain.Repositories.Interface;
using EMS.Infrastructure.Repositories.Implementations;
using EMS.Infrastructure.Integrations.UserManagement;
using EMS.Infrastructure.Persistence.Auditing;
using Pukar.Notifications.Application.Services;
using Pukar.Notifications.Domain.Repositories;
using Pukar.Usermanagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
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
            configuration.WriteTo.MongoDBBson(mongoLogs);
        }
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("EmsWeb", policy =>
        {
            var corsOrigins = builder.Configuration.GetSection(CorsOptions.SectionName).Get<string[]>()
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

            if (corsOrigins.Length == 0 && !builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must contain at least one origin outside Development.");
            }

            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        });
    });

    builder.Services.Configure<AuthorizationModeOptions>(builder.Configuration.GetSection(AuthorizationModeOptions.SectionName));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
    builder.Services.Configure<EmployeeSchedulingOptions>(
        builder.Configuration.GetSection(EmployeeSchedulingOptions.SectionName));
    builder.Services.Configure<EMS.API.Options.SeedAdminOptions>(
        builder.Configuration.GetSection(EMS.API.Options.SeedAdminOptions.SectionName));
    builder.Services.AddHostedService<EmsRbacSeedHostedService>();
    builder.Services.AddHostedService<RoleKeyBackfillHostedService>();
    builder.Services.AddHostedService<EmployeeScheduledChangeWorker>();
    builder.Services.AddHostedService<ScheduledChangeReminderWorker>();
    builder.Services.AddHostedService<DocumentExpiryNotificationWorker>();
    builder.Services.AddHostedService<IntegrationOutboxDispatcher>();
    // User Management seeding is triggered from the composition root but executes through
    // UM repositories/services. Idempotency cleanup supports the internal compatibility endpoints.
    builder.Services.AddHostedService<UserManagementSeedHostedService>();
    builder.Services.AddHostedService<UserManagementIdempotencyCleanupHostedService>();

    builder.Services.AddControllers(options =>
        {
            options.Filters.Add(
                new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        })
        // Serve the User Management controllers in-process from this single host.
        .AddApplicationPart(typeof(Pukar.Usermanagement.API.Controllers.AuthController).Assembly);
    builder.Services.AddOpenApi();

    // Compose the User Management bounded context in-process (own DbContext, repositories,
    // application services, hashing, JWT, SMTP, invitations, password reset). Symmetric
    // signing keeps token issuance and local validation aligned in one host.
    builder.Services.AddPukarUserManagement(builder.Configuration, connectionStringName: "UserManagementDb", useRsaSigning: false);

    builder.Services.AddUserManagementJwtAuthentication(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
    builder.Services.AddSingleton<AuthorizationTelemetry>();
    builder.Services.AddSingleton<IAuthorizationTelemetry>(sp => sp.GetRequiredService<AuthorizationTelemetry>());
    builder.Services.AddSingleton<IAuthorizationTelemetryReporter>(sp => sp.GetRequiredService<AuthorizationTelemetry>());
    builder.Services.AddScoped<IAuthorizationCutoverReadinessReporter, AuthorizationCutoverReadinessReporter>();
    builder.Services.AddScoped<IIdentityContext, HttpContextIdentityContext>();
    builder.Services.AddScoped<INotificationCurrentUserAccessor, HttpContextNotificationCurrentUserAccessor>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<EmployeeNotificationRecipientResolver>();
    builder.Services.AddScoped<IEmsNotificationProducer, EmsNotificationProducer>();
    builder.Services.AddScoped<IAuditContextAccessor, HttpContextAuditContextAccessor>();
    builder.Services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
    builder.Services.AddScoped<IRoleKeyPermissionService, RoleKeyPermissionService>();
    builder.Services.AddScoped<IRoleKeyCapabilityService, RoleKeyCapabilityService>();

    // In-process anti-corruption adapters replace the former EMS-to-UM HTTP + service-token
    // integration. They call User Management application services directly within this host.
    builder.Services.AddScoped<IUserManagementRoleMetadataClient, InProcessUserManagementRoleMetadataClient>();

    builder.Services.AddScoped<ILegacyRoleKeyGuard, LegacyRoleKeyGuard>();
    builder.Services.AddScoped<INavigationService, NavigationService>();
    builder.Services.AddScoped<IMenuService, MenuService>();
    builder.Services.AddScoped<EmployeeRelationshipValidator>();
    builder.Services.AddScoped<IEmployeeAccessService, EmployeeAccessService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IEmployeeDirectoryService, EmployeeDirectoryService>();
    builder.Services.AddScoped<IEmployeeLifecycleService, EmployeeLifecycleService>();
    builder.Services.AddScoped<IEmployeeTransferService, EmployeeTransferService>();
    builder.Services.AddScoped<IEmployeeNumberAllocator, SqlEmployeeNumberAllocator>();
    builder.Services.AddScoped<IEmployeeRoleSyncService, EmployeeRoleSyncService>();
    builder.Services.AddScoped<IEmployeeRoleService, EmployeeRoleService>();
    builder.Services.AddScoped<IEmployeeUserManagementGateway, InProcessEmployeeUserManagementGateway>();
    builder.Services.AddScoped<IEmployeeIdentityProvisioningService, EmployeeIdentityProvisioningService>();
    builder.Services.AddScoped<IEmployeeBusinessDateHelper, EmployeeBusinessDateHelper>();
    builder.Services.AddScoped<IEmployeeScheduledChangeService, EmployeeScheduledChangeService>();
    builder.Services.AddScoped<IEmployeeScheduledChangeApplier, EmployeeScheduledChangeApplier>();
    builder.Services.AddScoped<IEmployeeInvitationService, InProcessEmployeeInvitationService>();
    builder.Services.AddScoped<EMS.Application.Services.Integrations.IIntegrationOutboxWriter, EMS.Application.Services.Integrations.IntegrationOutboxWriter>();
    builder.Services.AddScoped<EMS.Application.Services.Integrations.IIntegrationOutboxProcessor, EMS.Application.Services.Integrations.IntegrationOutboxProcessor>();
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
    builder.Services.AddScoped<ILinkedEmployeeService, LinkedEmployeeService>();
    builder.Services.AddScoped<ILeaveEmployeeAccessService, LeaveEmployeeAccessService>();
    builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
    builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
    builder.Services.AddScoped<ILeavePolicyRuleService, LeavePolicyRuleService>();
    builder.Services.AddScoped<ILeaveAttachmentService, LeaveAttachmentService>();
    builder.Services.AddScoped<ILeaveAccrualService, LeaveAccrualService>();
    builder.Services.AddScoped<ILeaveImportService, LeaveImportService>();
    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<IOnboardingChecklistTemplateService, OnboardingChecklistTemplateService>();
    builder.Services.AddScoped<IOnboardingChecklistService, OnboardingChecklistService>();
    builder.Services.AddScoped<IShiftService, ShiftService>();
    builder.Services.AddScoped<IEmployeePortalService, EmployeePortalService>();
    builder.Services.AddScoped<IManagerTeamAccessService, ManagerTeamAccessService>();
    builder.Services.AddScoped<IManagerTeamService, ManagerTeamService>();
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
        .AddDbContextCheck<AppDbContext>(name: "database")
        // User Management now runs in-process; readiness is its own database rather than a remote host.
        .AddDbContextCheck<Pukar.Usermanagement.Domain.Database.UserManagementDbContext>(name: "user-management", tags: ["ready"])
        .AddCheck<RoleKeyMigrationHealthCheck>("role-key-migration", failureStatus: HealthStatus.Unhealthy, tags: ["ready"]);

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
        // Development convenience only: apply UM migrations first, then EMS migrations, each
        // against its own database. The destructive legacy [um] drop migration is
        // operator-gated inside the migration itself and will NOT run without an explicit
        // approval marker. Production applies migrations explicitly via the documented
        // `dotnet ef database update` commands.
        using (var scope = app.Services.CreateScope())
        {
            var umDb = scope.ServiceProvider.GetRequiredService<Pukar.Usermanagement.Domain.Database.UserManagementDbContext>();
            umDb.Database.Migrate();
            Log.Information("Applied pending EF Core migrations for UserManagementDbContext -> UserManagementDb (Development).");

            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appDb.Database.Migrate();
            Log.Information("Applied pending EF Core migrations for AppDbContext -> EMSDevDB (Development).");
        }

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
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready") || check.Name is "database" or "user-management" or "role-key-migration",
    }).AllowAnonymous();
    app.MapControllers();

    Log.Information("EMS.API starting ({Environment})", app.Environment.EnvironmentName);

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
