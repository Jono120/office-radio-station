namespace OfficeJukebox.Domain.Entities;

public sealed class TrackPlayVeto : Entity
{
    public Guid TrackPlayId { get; set; }
    public string ByUser { get; set; } = string.Empty;
    public DateTime VetoedAt { get; set; } = DateTime.UtcNow;
    public TrackPlay? TrackPlay { get; set; }
}
