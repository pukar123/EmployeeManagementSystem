using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.HasIndex(e => e.NormalizedEmail).IsUnique();

        builder.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(e => e.UserName).HasMaxLength(256);

        builder.Property(e => e.CreatedAtUtc).IsRequired();

        builder.HasMany(e => e.RefreshTokens)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
