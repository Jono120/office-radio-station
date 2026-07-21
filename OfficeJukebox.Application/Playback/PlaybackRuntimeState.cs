using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Playback;

public sealed class PlaybackRuntimeState
{
    private readonly object _lock = new();
    private TrackPlay? _currentlyPlaying;

    public TrackPlay? CurrentlyPlayingTrack
    {
        get { lock (_lock) { return _currentlyPlaying; } }
    }

    public PlaybackState? CurrentPlaybackState { get; private set; }

    public void SetCurrentTrack(TrackPlay? trackPlay)
    {
        lock (_lock) { _currentlyPlaying = trackPlay; }
    }

    public void SetPlaybackState(PlaybackState? state) => CurrentPlaybackState = state;

    public TrackPlay? GetCurrentTrack()
    {
        lock (_lock) { return _currentlyPlaying; }
    }
}
