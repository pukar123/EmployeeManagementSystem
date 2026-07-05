using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pukar.Usermanagement.Domain.DbModels;

namespace Pukar.Usermanagement.Domain.Database.Configurations;

public sealed class ServiceClientConfiguration : IEntityTypeConfiguration<ServiceClient>
{
    public void Configure(EntityTypeBuilder<ServiceClient> builder)
    {
        builder.ToTable("ServiceClients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SecretHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AllowedScopes).HasMaxLength(512).IsRequired();

        builder.HasIndex(x => x.ClientId).IsUnique();
    }
}
