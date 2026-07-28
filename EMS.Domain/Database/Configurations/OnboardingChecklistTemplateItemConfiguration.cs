using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class OnboardingChecklistTemplateItemConfiguration : IEntityTypeConfiguration<OnboardingChecklistTemplateItem>
{
    public void Configure(EntityTypeBuilder<OnboardingChecklistTemplateItem> builder)
    {
        builder.ToTable("OnboardingChecklistTemplateItems", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Category).HasConversion<int>();
        builder.Property(x => x.DefaultPriority).HasConversion<int?>();
        builder.Property(x => x.IsRequired).HasDefaultValue(true);

        builder.HasOne(x => x.Template)
            .WithMany(t => t.Items)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TemplateId, x.SortOrder });
    }
}
