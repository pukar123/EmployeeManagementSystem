using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeRetentionPolicyConfiguration : IEntityTypeConfiguration<EmployeeRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<EmployeeRetentionPolicy> builder)
    {
        builder.ToTable("EmployeeRetentionPolicies", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RetentionDays).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrganizationId).IsUnique();
    }
}
