using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeOnboardingChecklistConfiguration : IEntityTypeConfiguration<EmployeeOnboardingChecklist>
{
    public void Configure(EntityTypeBuilder<EmployeeOnboardingChecklist> builder)
    {
        builder.ToTable("EmployeeOnboardingChecklists", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GeneratedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Template)
            .WithMany(t => t.EmployeeChecklists)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeId).IsUnique();
        builder.HasIndex(x => x.TemplateId);
    }
}
