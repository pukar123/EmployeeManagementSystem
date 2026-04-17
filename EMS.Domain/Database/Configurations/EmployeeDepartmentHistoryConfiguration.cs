using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeDepartmentHistoryConfiguration : IEntityTypeConfiguration<EmployeeDepartmentHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeDepartmentHistory> builder)
    {
        builder.ToTable("EmployeeDepartmentHistories", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(1024);
        builder.Property(x => x.ChangedByUserName).HasMaxLength(256);
        builder.Property(x => x.ChangedByEmail).HasMaxLength(256);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.PreviousDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.NewDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.EffectiveFromUtc);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
