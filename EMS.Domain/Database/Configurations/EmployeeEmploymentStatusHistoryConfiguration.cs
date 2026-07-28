using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeEmploymentStatusHistoryConfiguration : IEntityTypeConfiguration<EmployeeEmploymentStatusHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeEmploymentStatusHistory> builder)
    {
        builder.ToTable("EmployeeEmploymentStatusHistories", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(1024);
        builder.Property(x => x.ChangedByUserName).HasMaxLength(256);
        builder.Property(x => x.ChangedByEmail).HasMaxLength(256);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.EffectiveDateUtc);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
