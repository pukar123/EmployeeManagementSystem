using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeSiteConfiguration : IEntityTypeConfiguration<EmployeeSite>
{
    public void Configure(EntityTypeBuilder<EmployeeSite> builder)
    {
        builder.ToTable("EmployeeSites", "org");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.EmployeeSites)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Site)
            .WithMany(x => x.EmployeeSites)
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.SiteId);
        builder.HasIndex(x => new { x.EmployeeId, x.SiteId }).IsUnique();
    }
}
