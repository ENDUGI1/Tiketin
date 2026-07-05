namespace Tiketin.Web.Domain;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Target minutes until first technician response.</summary>
    public int SlaResponseMinutes { get; set; }

    /// <summary>Target minutes until resolution.</summary>
    public int SlaResolutionMinutes { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
