namespace OfficeJukebox.Domain.Entities;

public sealed class SoundBoardEvent : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
