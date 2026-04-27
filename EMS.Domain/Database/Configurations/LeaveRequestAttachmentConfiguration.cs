using EMS.Domain.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Domain.Database.Configurations;

public sealed class LeaveRequestAttachmentConfiguration : IEntityTypeConfiguration<LeaveRequestAttachment>
{
    public void Configure(EntityTypeBuilder<LeaveRequestAttachment> builder)
    {
        builder.ToTable("LeaveRequestAttachments", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.LeaveRequest)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LeaveRequestId);
    }
}
