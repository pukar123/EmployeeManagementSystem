using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class LeavePolicyRuleConfiguration : IEntityTypeConfiguration<LeavePolicyRule>
{
    public void Configure(EntityTypeBuilder<LeavePolicyRule> builder)
    {
        builder.ToTable("LeavePolicyRules", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccrualRatePerPeriod).HasPrecision(10, 2);
        builder.Property(x => x.MaximumCarryForward).HasPrecision(10, 2);
        builder.Property(x => x.AccrualFrequency)
            .HasConversion<int>()
            .HasDefaultValue(AccrualFrequency.Monthly);
        builder.Property(x => x.EnableProration).HasDefaultValue(true);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.EffectiveFromDateUtc).HasColumnType("date");
        builder.Property(x => x.EffectiveToDateUtc).HasColumnType("date");
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LeaveType)
            .WithMany(x => x.PolicyRules)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.OrganizationId, x.LeaveTypeId, x.EffectiveFromDateUtc });
    }
}
