using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OpeningBalance).HasPrecision(10, 2);
        builder.Property(x => x.AccruedAmount).HasPrecision(10, 2);
        builder.Property(x => x.UsedAmount).HasPrecision(10, 2);
        builder.Property(x => x.AdjustedAmount).HasPrecision(10, 2);
        builder.Property(x => x.CarryForwardAmount).HasPrecision(10, 2);
        builder.Property(x => x.BalanceAsOfUtc).HasColumnType("date");
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LeaveType)
            .WithMany(x => x.LeaveBalances)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.BalanceAsOfUtc });
    }
}
