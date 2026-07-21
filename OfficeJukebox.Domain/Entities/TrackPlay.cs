namespace OfficeJukebox.Domain.Entities;

public sealed class TrackPlay : Entity
{
    public string User { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public bool Excluded { get; set; }
    public bool IsSkipped { get; set; }
    public string? Reason { get; set; }
    public string TrackJson { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalLink { get; set; }
    public TrackPlayStatus Status { get; set; } = TrackPlayStatus.Queued;
    public Track Track { get; set; } = new();
    public ICollection<TrackPlayVeto> Vetoes { get; set; } = new List<TrackPlayVeto>();
    public ICollection<TrackPlayLike> Likes { get; set; } = new List<TrackPlayLike>();
    public int VetoCount => Vetoes.Count;
    public int LikeCount => Likes.Count;
}
