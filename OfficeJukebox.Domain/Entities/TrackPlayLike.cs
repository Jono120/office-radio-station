namespace OfficeJukebox.Domain.Entities;

public sealed class TrackPlayLike : Entity
{
    public Guid TrackPlayId { get; set; }
    public string ByUser { get; set; } = string.Empty;
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    public TrackPlay? TrackPlay { get; set; }
}
