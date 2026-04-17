using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeePositionHistoryConfiguration : IEntityTypeConfiguration<EmployeePositionHistory>
{
    public void Configure(EntityTypeBuilder<EmployeePositionHistory> builder)
    {
        builder.ToTable("EmployeePositionHistories", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(1024);
        builder.Property(x => x.ChangedByUserName).HasMaxLength(256);
        builder.Property(x => x.ChangedByEmail).HasMaxLength(256);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<JobPosition>()
            .WithMany()
            .HasForeignKey(x => x.PreviousJobPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JobPosition>()
            .WithMany()
            .HasForeignKey(x => x.NewJobPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.EffectiveFromUtc);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
