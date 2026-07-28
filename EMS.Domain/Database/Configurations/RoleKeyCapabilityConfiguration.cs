using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class RoleKeyCapabilityConfiguration : IEntityTypeConfiguration<RoleKeyCapability>
{
    public void Configure(EntityTypeBuilder<RoleKeyCapability> builder)
    {
        builder.ToTable("RoleKeyCapabilities", "ems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CapabilityKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => new { x.RoleKey, x.CapabilityKey }).IsUnique();
    }
}
