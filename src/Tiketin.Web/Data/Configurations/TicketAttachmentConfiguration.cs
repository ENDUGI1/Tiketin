using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Data.Configurations;

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(a => a.Ticket)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Comment)
            .WithMany(c => c.Attachments)
            .HasForeignKey(a => a.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.UploadedBy)
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
