using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.Property(t => t.TicketNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(t => t.TicketNumber).IsUnique();

        builder.Property(t => t.Title).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Description).IsRequired();

        builder.Property(t => t.Priority).HasConversion<short>();
        builder.Property(t => t.Status).HasConversion<short>().HasDefaultValue(TicketStatus.Open);

        builder.Property(t => t.SatisfactionNote).HasMaxLength(300);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Reporter)
            .WithMany()
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.Status, t.Priority })
            .IsDescending(false, true)
            .HasDatabaseName("ix_tickets_status_priority");

        builder.HasIndex(t => t.AssigneeId)
            .HasFilter("assignee_id IS NOT NULL")
            .HasDatabaseName("ix_tickets_assignee");

        builder.HasIndex(t => t.ReporterId)
            .HasDatabaseName("ix_tickets_reporter");

        builder.HasIndex(t => t.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_tickets_created_at");
    }
}
