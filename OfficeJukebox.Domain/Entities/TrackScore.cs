namespace OfficeJukebox.Domain.Entities;

public sealed class TrackScore : Entity
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string ExternalLink { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsExcluded { get; set; }
    public double MillisecondsSinceLastPlay { get; set; }
    public string TrackJson { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
