using System.Reflection;
using NetArchTest.Rules;

namespace EMS.ArchitectureTests;

/// <summary>
/// Enforces the single-host / two-bounded-context boundaries. EMS.API is the composition
/// root and may reference User Management implementation projects; the inner EMS layers may
/// not. Neither host may depend on the (decommissioned) UM Host project.
/// </summary>
public sealed class UserManagementBoundaryTests
{
    private static readonly Assembly Domain = typeof(EMS.Domain.Database.AppDbContext).Assembly;
    private static readonly Assembly Application = typeof(EMS.Application.Services.Employees.IEmployeeUserManagementGateway).Assembly;
    private static readonly Assembly Infrastructure = typeof(EMS.Infrastructure.Integrations.UserManagement.InProcessEmployeeUserManagementGateway).Assembly;
    private static readonly Assembly Api = typeof(EMS.API.Auth.UserManagementJwtExtensions).Assembly;

    private const string UmApi = "Pukar.Usermanagement.API";
    private const string UmApplication = "Pukar.Usermanagement.Application";
    private const string UmDomain = "Pukar.Usermanagement.Domain";
    private const string UmInfrastructure = "Pukar.Usermanagement.Infrastructure";
    private const string UmHost = "Pukar.Usermanagement.Host";
    private const string UmContracts = "Pukar.Usermanagement.Contracts";

    [Test]
    public void Domain_DoesNotReference_AnyUserManagementImplementation()
    {
        // EMS.Domain must stay free of every UM assembly except (optionally) Contracts.
        AssertNoReferences(Domain, UmApi, UmApplication, UmDomain, UmInfrastructure, UmHost);
    }

    [Test]
    public void Application_DoesNotReference_UserManagementImplementation()
    {
        // EMS.Application may use stable Contracts but never UM implementation layers.
        AssertNoReferences(Application, UmApi, UmApplication, UmDomain, UmInfrastructure, UmHost);
    }

    [Test]
    public void Application_DoesNotReference_Infrastructure()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("EMS.Infrastructure")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailures(result));
    }

    [Test]
    public void Infrastructure_MayReference_UmApplicationAndDomain_ButNotApiOrHost()
    {
        // In-process adapters call UM application services directly, so referencing
        // UM Application/Domain/Contracts is expected — but never UM controllers or the host.
        AssertNoReferences(Infrastructure, UmApi, UmHost);

        var referenced = Infrastructure.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(referenced, Does.Contain(UmContracts));
        Assert.That(referenced, Does.Contain(UmApplication));
    }

    [Test]
    public void Api_IsCompositionRoot_ButDoesNotReference_UmHost()
    {
        // EMS.API composes both bounded contexts; the only forbidden UM reference is the
        // decommissioned standalone host.
        AssertNoReferences(Api, UmHost);

        var referenced = Api.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(referenced, Does.Contain(UmApplication));
        Assert.That(referenced, Does.Contain(UmInfrastructure));
    }

    [Test]
    public void EmsControllers_DoNotDependOn_DbContextDirectly()
    {
        var result = Types.InAssembly(Api)
            .That()
            .ResideInNamespace("EMS.API.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailures(result));
    }

    private static void AssertNoReferences(Assembly assembly, params string[] forbidden)
    {
        var referenced = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.Ordinal);

        var violations = forbidden.Where(referenced.Contains).ToList();
        Assert.That(
            violations,
            Is.Empty,
            $"{assembly.GetName().Name} must not reference: {string.Join(", ", violations)}");
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypeNames is null || result.FailingTypeNames.Count == 0)
            return "Architecture rule failed.";

        return "Architecture rule failed for: " + string.Join(", ", result.FailingTypeNames);
    }
}
