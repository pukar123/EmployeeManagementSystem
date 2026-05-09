using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class PositionRoleConfiguration : IEntityTypeConfiguration<PositionRole>
{
    public void Configure(EntityTypeBuilder<PositionRole> builder)
    {
        builder.ToTable("PositionRoles", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.JobPosition)
            .WithMany(x => x.PositionRoles)
            .HasForeignKey(x => x.JobPositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.JobPositionId, x.RoleId })
            .IsUnique();

        builder.HasIndex(x => x.RoleId);
    }
}
