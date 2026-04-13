using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkDate).HasColumnType("date");
        builder.Property(x => x.CheckInLatitude).HasPrecision(9, 6);
        builder.Property(x => x.CheckInLongitude).HasPrecision(9, 6);
        builder.Property(x => x.CheckOutLatitude).HasPrecision(9, 6);
        builder.Property(x => x.CheckOutLongitude).HasPrecision(9, 6);

        builder.Property(x => x.Source)
            .HasConversion<int>()
            .HasDefaultValue(AttendanceSource.Web);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(AttendanceStatus.Open);

        builder.Property(x => x.ManualReason).HasMaxLength(500);

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

        builder.HasMany(x => x.Breaks)
            .WithOne(x => x.AttendanceRecord)
            .HasForeignKey(x => x.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.WorkDate });
        builder.HasIndex(x => new { x.EmployeeId, x.CheckOutAtUtc });
    }
}
