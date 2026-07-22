using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Abstractions;

public interface IMusicPlayer
{
    TrackPlay? CurrentlyPlayingTrack { get; }
    PlaybackState? CurrentPlaybackState { get; }
}

public interface IPlaybackOrchestrator : IMusicPlayer
{
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Skips the identified track (currently playing or still queued). Returns false when no such track exists.</summary>
    Task<bool> SkipAsync(Guid trackPlayId, string user, CancellationToken cancellationToken = default);

    /// <summary>Records a veto against the identified track (currently playing or still queued). Returns false when no such track exists.</summary>
    Task<bool> VetoAsync(Guid trackPlayId, string user, CancellationToken cancellationToken = default);
}
