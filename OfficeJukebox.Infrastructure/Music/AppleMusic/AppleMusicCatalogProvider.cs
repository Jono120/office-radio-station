using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Infrastructure.Music.AppleMusic;

public sealed class AppleMusicCatalogProvider : IMusicCatalogProvider, IMusicPlaybackController
{
    public string ProviderId => "apple-music";
    public ProviderCapabilities Capabilities =>
        ProviderCapabilities.Search | ProviderCapabilities.Resolve | ProviderCapabilities.RequiresAuth;

    public Task<IReadOnlyList<Track>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Track>>([]);

    public Task<Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default) =>
        Task.FromResult(new Track { Name = trackRef.ExternalId, Link = $"https://music.apple.com/track/{trackRef.ExternalId}" });

    public Task<PlaybackState> GetStateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new PlaybackState(false, 0, 0, null, null, null));

    public Task PlayAsync(TrackRef trackRef, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Apple Music device playback is not yet implemented.");

    public Task PauseAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Apple Music device playback is not yet implemented.");

    public Task ResumeAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Apple Music device playback is not yet implemented.");

    public Task SkipAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Apple Music device playback is not yet implemented.");

    public Task<IReadOnlyList<PlaybackDevice>> ListDevicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PlaybackDevice>>([]);

    public Task SetActiveDeviceAsync(string deviceId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Apple Music device playback is not yet implemented.");
}
