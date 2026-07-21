using System.Text.Json;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Serialization;

public static class TrackJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize(Track track) => JsonSerializer.Serialize(track, Options);

    public static Track Deserialize(string trackJson)
    {
        if (string.IsNullOrWhiteSpace(trackJson))
        {
            return new Track();
        }

        return JsonSerializer.Deserialize<Track>(trackJson, Options) ?? new Track();
    }

    public static void SyncTrackJson(TrackPlay trackPlay)
    {
        trackPlay.TrackJson = Serialize(trackPlay.Track);
    }

    public static void HydrateTrack(TrackPlay trackPlay)
    {
        if (!string.IsNullOrWhiteSpace(trackPlay.TrackJson))
        {
            trackPlay.Track = Deserialize(trackPlay.TrackJson);
        }
        else if (!string.IsNullOrWhiteSpace(trackPlay.Track.Name))
        {
            SyncTrackJson(trackPlay);
        }
    }
}
