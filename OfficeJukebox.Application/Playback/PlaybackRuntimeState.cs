using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Playback;

/// <summary>
/// Singleton runtime state shared by the playback loop and request handlers.
/// All state transitions (dequeue → play, skip, complete) must run inside
/// <see cref="LockAsync"/> so the check-then-act sequences in the orchestrator
/// are serialized. Plain reads (now-playing endpoint, queue rules) are safe
/// without the lock: they observe a single reference atomically.
/// </summary>
public sealed class PlaybackRuntimeState
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public TrackPlay? CurrentlyPlayingTrack { get; private set; }

    public PlaybackState? CurrentPlaybackState { get; private set; }

    public async Task<Releaser> LockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Releaser(_gate);
    }

    public TrackPlay? GetCurrentTrack() => CurrentlyPlayingTrack;

    public void SetCurrentTrack(TrackPlay? trackPlay) => CurrentlyPlayingTrack = trackPlay;

    public void SetPlaybackState(PlaybackState? state) => CurrentPlaybackState = state;

    public readonly struct Releaser(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
