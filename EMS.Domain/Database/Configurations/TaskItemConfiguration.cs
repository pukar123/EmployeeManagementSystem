using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.Priority)
            .HasConversion<int?>();

        builder.Property(x => x.AssignedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.TaskItems)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OnboardingTemplateItem)
            .WithMany(i => i.GeneratedTasks)
            .HasForeignKey(x => x.OnboardingTemplateItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EmployeeOnboardingChecklist)
            .WithMany(c => c.Tasks)
            .HasForeignKey(x => x.EmployeeOnboardingChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.StartAtUtc);
        builder.HasIndex(x => x.DueAtUtc);
        builder.HasIndex(x => x.OnboardingTemplateItemId);
        builder.HasIndex(x => x.EmployeeOnboardingChecklistId);
    }
}
