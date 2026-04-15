using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class RoleKeyPermissionConfiguration : IEntityTypeConfiguration<RoleKeyPermission>
{
    public void Configure(EntityTypeBuilder<RoleKeyPermission> builder)
    {
        builder.ToTable("RoleKeyPermissions", "ems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => new { x.RoleKey, x.MenuId }).IsUnique();

        builder.HasOne(x => x.Menu)
            .WithMany(x => x.RoleKeyPermissions)
            .HasForeignKey(x => x.MenuId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
