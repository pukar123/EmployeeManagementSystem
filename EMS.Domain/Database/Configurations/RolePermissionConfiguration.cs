using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "ems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleId).IsRequired();

        builder.HasIndex(x => new { x.RoleId, x.MenuId }).IsUnique();

        builder.HasOne(x => x.Menu)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // RoleId references um.Roles — enforced in app layer; optional DB FK via manual migration if desired.
    }
}
