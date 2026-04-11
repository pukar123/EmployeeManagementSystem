using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites", "org");

        builder.HasKey(x => x.SiteId);

        builder.Property(x => x.SiteName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SiteDescription).HasMaxLength(2000);

        builder.Property(x => x.SiteLocation)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(x => x.IsDeleted);
    }
}
