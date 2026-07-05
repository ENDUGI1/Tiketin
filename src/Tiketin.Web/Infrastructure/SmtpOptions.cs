namespace Tiketin.Web.Infrastructure;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "helpdesk@tiketin.local";
    public string FromName { get; set; } = "Tiketin Helpdesk";
    public bool UseStartTls { get; set; }
}
