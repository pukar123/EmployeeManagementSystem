using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("EmployeeDocuments", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.EmployeeDocuments)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Document)
            .WithMany(x => x.EmployeeDocuments)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.EmployeeId, x.DocumentId }).IsUnique();
    }
}
