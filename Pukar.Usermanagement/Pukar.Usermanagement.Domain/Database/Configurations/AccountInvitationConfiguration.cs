using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Database.Configurations;

public sealed class AccountInvitationConfiguration : IEntityTypeConfiguration<AccountInvitation>
{
    public void Configure(EntityTypeBuilder<AccountInvitation> builder)
    {
        builder.ToTable("AccountInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalCorrelationId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RecipientEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.RecipientDisplayName).HasMaxLength(256);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DeliveryFailureReason).HasMaxLength(2000);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.ExternalCorrelationId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
