using EMS.API.Options;
using EMS.Domain.Database.Migrations;

namespace EMS.ArchitectureTests;

public sealed class ConfigurationAndMigrationTests
{
    private static string ReadMigrationSource(string fileName)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(repoRoot, "EMS.Domain", "Database", "Migrations", fileName));
    }

    [Test]
    public void CorsOptions_SectionName_IsCors()
    {
        Assert.That(CorsOptions.SectionName, Is.EqualTo("Cors"));
    }

    [Test]
    public void DropEmployeeInvitationsMigration_PreservesSourceTableInUp()
    {
        var body = ReadMigrationSource("20260703083655_DropEmployeeInvitations.cs");
        Assert.That(body, Does.Not.Contain("DROP TABLE [emp].[EmployeeInvitations]"));
        Assert.That(body, Does.Contain("_UmSeparationCutoverMarker"));
    }

    [Test]
    public void SoakPeriodMigration_DropsEmployeeInvitationsWhenPresent()
    {
        var body = ReadMigrationSource("20260705073448_DropEmployeeInvitationsAfterSoakPeriod.cs");
        Assert.That(body, Does.Contain("EmployeeInvitations"));
    }
}
