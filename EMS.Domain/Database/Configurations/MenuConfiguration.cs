using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menus", "ems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.Key).IsUnique();

        builder.Property(x => x.Label).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RoutePath).IsRequired().HasMaxLength(400);
        builder.Property(x => x.IconKey).HasMaxLength(64);

        builder.HasOne(x => x.ParentMenu)
            .WithMany(x => x.ChildMenus)
            .HasForeignKey(x => x.ParentMenuId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
