using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class AuditTrailEntryConfiguration : IEntityTypeConfiguration<AuditTrailEntry>
{
    public void Configure(EntityTypeBuilder<AuditTrailEntry> builder)
    {
        builder.ToTable("AuditTrailEntries", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(256);
        builder.Property(x => x.EntityKey).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(32);
        builder.Property(x => x.ActorEmail).HasMaxLength(256);
        builder.Property(x => x.ActorUserName).HasMaxLength(256);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.ChangedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(x => x.EntityName);
        builder.HasIndex(x => x.EntityKey);
        builder.HasIndex(x => x.ChangedAtUtc);
        builder.HasIndex(x => x.ActorUserId);
    }
}
