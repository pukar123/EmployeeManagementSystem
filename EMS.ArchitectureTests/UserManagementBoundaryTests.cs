using System.Reflection;
using NetArchTest.Rules;

namespace EMS.ArchitectureTests;

public sealed class UserManagementBoundaryTests
{
    private static readonly Assembly Domain = typeof(EMS.Domain.Database.AppDbContext).Assembly;
    private static readonly Assembly Application = typeof(EMS.Application.Services.Employees.IEmployeeUserManagementGateway).Assembly;
    private static readonly Assembly Infrastructure = typeof(EMS.Infrastructure.Integrations.UserManagement.HttpEmployeeUserManagementGateway).Assembly;
    private static readonly Assembly Api = typeof(EMS.API.Auth.UserManagementJwtExtensions).Assembly;

    private static readonly string[] ForbiddenUmAssemblies =
    [
        "Pukar.Usermanagement.API",
        "Pukar.Usermanagement.Application",
        "Pukar.Usermanagement.Domain",
        "Pukar.Usermanagement.Infrastructure",
        "Pukar.Usermanagement.Host",
    ];

    [Test]
    public void Domain_DoesNotReference_UserManagementImplementationAssemblies()
    {
        AssertNoForbiddenReferences(Domain);
    }

    [Test]
    public void Application_DoesNotReference_UserManagementImplementationAssemblies()
    {
        AssertNoForbiddenReferences(Application);
    }

    [Test]
    public void Infrastructure_DoesNotReference_UserManagementImplementationAssemblies()
    {
        AssertNoForbiddenReferences(Infrastructure);

        var referenced = Infrastructure.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        Assert.That(referenced, Does.Contain("Pukar.Usermanagement.Contracts"));
    }

    [Test]
    public void Api_DoesNotReference_UserManagementImplementationAssemblies()
    {
        AssertNoForbiddenReferences(Api);
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
    public void Controllers_DoNotDependOn_DbContext()
    {
        var result = Types.InAssembly(Api)
            .That()
            .ResideInNamespace("EMS.API.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailures(result));
    }

    private static void AssertNoForbiddenReferences(Assembly assembly)
    {
        var referenced = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.Ordinal);

        var violations = ForbiddenUmAssemblies.Where(referenced.Contains).ToList();
        Assert.That(
            violations,
            Is.Empty,
            $"{assembly.GetName().Name} must not reference User Management implementation assemblies: {string.Join(", ", violations)}");
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypeNames is null || result.FailingTypeNames.Count == 0)
            return "Architecture rule failed.";

        return "Architecture rule failed for: " + string.Join(", ", result.FailingTypeNames);
    }
}
