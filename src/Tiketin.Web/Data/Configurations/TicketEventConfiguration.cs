using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Data.Configurations;

public class TicketEventConfiguration : IEntityTypeConfiguration<TicketEvent>
{
    public void Configure(EntityTypeBuilder<TicketEvent> builder)
    {
        builder.Property(e => e.EventType).HasConversion<short>();
        builder.Property(e => e.OldValue).HasMaxLength(120);
        builder.Property(e => e.NewValue).HasMaxLength(120);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(e => e.Ticket)
            .WithMany(t => t.Events)
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TicketId, e.CreatedAt })
            .HasDatabaseName("ix_ticket_events_ticket");
    }
}
