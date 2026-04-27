using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartDateUtc).HasColumnType("date");
        builder.Property(x => x.EndDateUtc).HasColumnType("date");
        builder.Property(x => x.Unit)
            .HasConversion<int>()
            .HasDefaultValue(LeaveUnit.Days);
        builder.Property(x => x.RequestedAmount).HasPrecision(10, 2);
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(LeaveRequestStatus.Pending);
        builder.Property(x => x.SubmittedAtUtc).HasDefaultValueSql("GETUTCDATE()");
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
            .WithMany(x => x.LeaveRequests)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.LeaveRequest)
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.StartDateUtc, x.EndDateUtc });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
    }
}
