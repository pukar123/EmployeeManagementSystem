using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeRoleAssignmentConfiguration : IEntityTypeConfiguration<EmployeeRoleAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeRoleAssignment> builder)
    {
        builder.ToTable(
            "EmployeeRoleAssignments",
            "org",
            t => t.HasCheckConstraint(
                "CK_EmployeeRoleAssignments_SourceJobPosition",
                "([Source] = 1 AND [JobPositionId] IS NOT NULL) OR ([Source] = 2 AND [JobPositionId] IS NULL)"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Source)
            .HasConversion<int>();

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.RoleAssignments)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.JobPosition)
            .WithMany(x => x.EmployeeRoleAssignments)
            .HasForeignKey(x => x.JobPositionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.EmployeeId, x.RoleId, x.Source, x.JobPositionId })
            .IsUnique()
            .HasFilter("[Source] = 1 AND [JobPositionId] IS NOT NULL");

        builder.HasIndex(x => new { x.EmployeeId, x.RoleId, x.Source })
            .IsUnique()
            .HasFilter("[Source] = 2 AND [JobPositionId] IS NULL");

        builder.HasIndex(x => new { x.EmployeeId, x.Source });
        builder.HasIndex(x => x.JobPositionId);

    }
}
