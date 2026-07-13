using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pukar.Notifications.Domain.DbModels;
using Pukar.Notifications.Domain.Enums;

namespace EMS.Domain.Database.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "ntf");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipientKey).HasMaxLength(128);
        builder.Property(x => x.TypeKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ActionUrl).HasMaxLength(512);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000);
        builder.Property(x => x.DedupeKey).HasMaxLength(256);
        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(NotificationSeverity.Normal);

        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.RecipientUserId, x.DedupeKey })
            .IsUnique()
            .HasFilter("[DedupeKey] IS NOT NULL AND [IsArchived] = 0");
    }
}
