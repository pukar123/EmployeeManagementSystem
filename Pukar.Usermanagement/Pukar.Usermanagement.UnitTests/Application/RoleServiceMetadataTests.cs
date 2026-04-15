using Pukar.Usermanagement.Application.Services.Roles;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Pukar.Usermanagement.UnitTests.Application;

[TestFixture]
public class RoleServiceMetadataTests
{
    [Test]
    public async Task GetMetadataV1Async_ReturnsOrderedRoleMetadataWithNormalizedNames()
    {
        var options = new DbContextOptionsBuilder<UserManagementDbContext>()
            .UseInMemoryDatabase($"roles-metadata-{Guid.NewGuid()}")
            .Options;

        await using var db = new UserManagementDbContext(options);
        db.Roles.AddRange(new List<Role>
        {
            new() { Id = 2, Name = "User", NormalizedName = "USER", IsSystem = false },
            new() { Id = 1, Name = "Admin", NormalizedName = "ADMIN", IsSystem = true },
        });
        await db.SaveChangesAsync();

        var service = new RoleService(new RoleRepository(db));
        var result = await service.GetMetadataV1Async();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Admin"));
        Assert.That(result[0].NormalizedName, Is.EqualTo("ADMIN"));
        Assert.That(result[0].IsSystem, Is.True);
        Assert.That(result[1].Name, Is.EqualTo("User"));
        Assert.That(result[1].NormalizedName, Is.EqualTo("USER"));
        Assert.That(result[1].IsSystem, Is.False);
    }
}
