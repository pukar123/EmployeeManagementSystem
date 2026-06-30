using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeScheduledChangeConfiguration : IEntityTypeConfiguration<EmployeeScheduledChange>
{
    public void Configure(EntityTypeBuilder<EmployeeScheduledChange> builder)
    {
        builder.ToTable("EmployeeScheduledChanges", "emp");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(256);
        builder.Property(x => x.CreatedByEmail).HasMaxLength(256);
        builder.Property(x => x.FailureReason).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.EmployeeId, x.ChangeType, x.Status })
            .HasFilter($"[{nameof(EmployeeScheduledChange.Status)}] = {(int)EmployeeScheduledChangeStatus.Pending}")
            .IsUnique();

        builder.HasIndex(x => x.EffectiveAtUtc);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
