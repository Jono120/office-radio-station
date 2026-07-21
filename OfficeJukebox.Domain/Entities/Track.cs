namespace OfficeJukebox.Domain.Entities;

public sealed class Track
{
    public string Name { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public Album? Album { get; set; }
    public string ArtistsJson { get; set; } = "[]";
    public IEnumerable<Artist> Artists
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<Artist>>(ArtistsJson) ?? [];
        set => ArtistsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
    public long DurationMilliseconds { get; set; }
    public string? TrackArtworkUrl { get; set; }
}
