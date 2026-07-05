using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Database.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Route).HasMaxLength(512).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RequestFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ContentType).HasMaxLength(128);

        builder.HasIndex(x => new { x.ClientId, x.Route, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
