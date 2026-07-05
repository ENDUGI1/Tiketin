using System.Net;
using Tiketin.Web.Domain;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class EmailNotificationService(
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<EmailNotificationService> logger) : INotificationService
{
    private string BaseUrl => configuration["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";

    public async Task TicketAssignedAsync(Ticket ticket, AppUser assignee, CancellationToken ct = default)
    {
        var subject = $"[{ticket.TicketNumber}] Tiket ditugaskan ke Anda";
        var body = $"""
            <p>Halo {WebUtility.HtmlEncode(assignee.FullName)},</p>
            <p>Tiket <strong>{ticket.TicketNumber}</strong> ditugaskan ke Anda:</p>
            <p><strong>{WebUtility.HtmlEncode(ticket.Title)}</strong></p>
            <p><a href="{BaseUrl}/tickets/{ticket.Id}">Buka tiket</a></p>
            """;

        await SendSafeAsync(assignee, subject, body, ct);
    }

    public async Task TicketResolvedAsync(Ticket ticket, AppUser reporter, CancellationToken ct = default)
    {
        var subject = $"[{ticket.TicketNumber}] Tiket Anda selesai dikerjakan";
        var body = $"""
            <p>Halo {WebUtility.HtmlEncode(reporter.FullName)},</p>
            <p>Tiket <strong>{ticket.TicketNumber}</strong> telah diselesaikan:</p>
            <p><strong>{WebUtility.HtmlEncode(ticket.Title)}</strong></p>
            <p>Jika masalah belum teratasi, Anda bisa membuka ulang tiket dalam 7 hari.
               Jangan lupa memberi penilaian atas penanganan tiket ini.</p>
            <p><a href="{BaseUrl}/tickets/{ticket.Id}">Buka tiket</a></p>
            """;

        await SendSafeAsync(reporter, subject, body, ct);
    }

    private async Task SendSafeAsync(AppUser recipient, string subject, string body, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(recipient.Email))
        {
            return;
        }

        try
        {
            await emailSender.SendAsync(recipient.Email, recipient.FullName, subject, body, ct);
        }
        catch (Exception ex)
        {
            // Notification failure must never fail the ticket operation.
            logger.LogWarning(ex, "Failed to send notification '{Subject}' to {Email}", subject, recipient.Email);
        }
    }
}
