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
    Task SkipCurrentAsync(string user, CancellationToken cancellationToken = default);
    Task VetoCurrentAsync(string user, CancellationToken cancellationToken = default);
}
