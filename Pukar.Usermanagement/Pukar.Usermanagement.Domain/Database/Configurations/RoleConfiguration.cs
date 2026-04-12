using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Database.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(128).IsRequired();
        builder.Property(e => e.NormalizedName).HasMaxLength(128).IsRequired();
        builder.HasIndex(e => e.NormalizedName).IsUnique();

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasMany(e => e.UserRoles)
            .WithOne(e => e.Role)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
