using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class OrganizationEmployeeNumberSequenceConfiguration : IEntityTypeConfiguration<OrganizationEmployeeNumberSequence>
{
    public void Configure(EntityTypeBuilder<OrganizationEmployeeNumberSequence> builder)
    {
        builder.ToTable("OrganizationEmployeeNumberSequences", "org");

        builder.HasKey(x => x.OrganizationId);

        builder.Property(x => x.LastAllocatedNumber)
            .IsRequired();

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
