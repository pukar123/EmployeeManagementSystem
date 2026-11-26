using EMS.API.Auth;
using EMS.API.Bootstrap;
using EMS.API.Middleware;
using EMS.API.Options;
using EMS.API.Services;
using EMS.Application.Options;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Departments;
using EMS.Application.Services.Documents;
using EMS.Application.Services.Files;
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
using Pukar.Usermanagement.API.Extensions;
using Pukar.Usermanagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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

            // Retry-After and Content-Disposition are not CORS-safelisted response headers. Expose them so the web
            // app can read how long to wait after a 429 from the auth endpoints, and the file name of a download.
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After", "Content-Disposition");
        });
    });

    builder.Services.Configure<AuthorizationModeOptions>(builder.Configuration.GetSection(AuthorizationModeOptions.SectionName));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
    builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

    // Private uploads (documents, leave attachments) live outside the web root. Resolve and check the folder now,
    // so a private root inside wwwroot stops the API before it can serve anything.
    var webRootPath = builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    var storageOptions = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
    var privateRootPath = PrivateFileStore.ResolveRoot(storageOptions.PrivateRoot, builder.Environment.ContentRootPath, webRootPath);
    Directory.CreateDirectory(Path.Combine(privateRootPath, PrivateFileStore.EmployeeFolder));
    Directory.CreateDirectory(Path.Combine(privateRootPath, PrivateFileStore.LeaveFolder));
    builder.Services.AddSingleton(sp => new PrivateFileStore(privateRootPath, sp.GetRequiredService<ILogger<PrivateFileStore>>()));
    builder.Services.AddScoped<PublicToPrivateFileMover>();
    builder.Services.Configure<EmployeeSchedulingOptions>(
        builder.Configuration.GetSection(EmployeeSchedulingOptions.SectionName));
    builder.Services.Configure<EMS.API.Options.SeedAdminOptions>(
        builder.Configuration.GetSection(EMS.API.Options.SeedAdminOptions.SectionName));
    builder.Services.AddHostedService<EmsRbacSeedHostedService>();
    builder.Services.AddHostedService<RoleKeyBackfillHostedService>();
    builder.Services.AddHostedService<PrivateFileMoveHostedService>();
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

    // Sign-in, registration and password-recovery limits (AuthRateLimiting section). Per-IP limits use
    // the connection address only; forwarded headers are not trusted (no reverse proxy is configured).
    builder.Services.AddPukarUserManagementRateLimiting(builder.Configuration);

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
    builder.Services.AddScoped<IDocumentAdminAccessService, DocumentAdminAccessService>();
    builder.Services.AddScoped<IDocumentAccessService, DocumentAccessService>();
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
    builder.Services.AddScoped<IAttendanceAdminAccessService, AttendanceAdminAccessService>();
    builder.Services.AddScoped<IAttendanceEmployeeAccessService, AttendanceEmployeeAccessService>();
    builder.Services.AddScoped<IAttendanceService, AttendanceService>();
    builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
    builder.Services.AddScoped<ILinkedEmployeeService, LinkedEmployeeService>();
    builder.Services.AddScoped<ILeaveAdminAccessService, LeaveAdminAccessService>();
    builder.Services.AddScoped<ILeaveEmployeeAccessService, LeaveEmployeeAccessService>();
    builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
    builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
    builder.Services.AddScoped<ILeaveApprovalAccessService, LeaveApprovalAccessService>();
    builder.Services.AddScoped<ILeaveApprovalService, LeaveApprovalService>();
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
    // B10: one shared, stateless upload checker for every upload.
    builder.Services.AddSingleton<IUploadFileChecker, UploadFileChecker>();
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

    var logoRootPath = Path.Combine(webRootPath, "attachments", "Organization");
    Directory.CreateDirectory(logoRootPath);

    // Only public organisation logos are served as static files, so this may run before authentication.
    // Nothing else under wwwroot is served: other paths fall through to routing and return 404.
    // B10: only raster images are served (PNG, JPEG, WebP, and GIF for logos saved before B10). Every other
    // extension, including .svg, is not in the map and ServeUnknownFileTypes stays false, so it returns 404.
    var logoContentTypes = new FileExtensionContentTypeProvider();
    logoContentTypes.Mappings.Clear();
    logoContentTypes.Mappings[".png"] = "image/png";
    logoContentTypes.Mappings[".jpg"] = "image/jpeg";
    logoContentTypes.Mappings[".jpeg"] = "image/jpeg";
    logoContentTypes.Mappings[".webp"] = "image/webp";
    logoContentTypes.Mappings[".gif"] = "image/gif";
    app.UseStaticFiles(new StaticFileOptions
    {
        RequestPath = "/attachments/Organization",
        FileProvider = new PhysicalFileProvider(logoRootPath),
        ContentTypeProvider = logoContentTypes,
        ServeUnknownFileTypes = false,
        OnPrepareResponse = context =>
            context.Context.Response.Headers[FileDownloadHeaders.NoSniffHeader] = FileDownloadHeaders.NoSniffValue,
    });

    app.UseCors("EmsWeb");

    // After CORS so 429 responses carry CORS headers and preflights are never counted.
    app.UsePukarUserManagementRateLimiting();

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

public partial class Program;
