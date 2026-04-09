using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.StoredRelativePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.FileKind)
            .HasConversion<int>();

        builder.Property(x => x.IssueDate)
            .HasColumnType("date");

        builder.Property(x => x.ExpiryDate)
            .HasColumnType("date");

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DocumentType)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
