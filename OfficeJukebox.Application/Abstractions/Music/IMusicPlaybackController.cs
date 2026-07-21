using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Abstractions.Music;

public interface IMusicPlaybackController : IMusicProvider
{
    Task<PlaybackState> GetStateAsync(CancellationToken cancellationToken = default);
    Task PlayAsync(TrackRef trackRef, CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task SkipAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaybackDevice>> ListDevicesAsync(CancellationToken cancellationToken = default);
    Task SetActiveDeviceAsync(string deviceId, CancellationToken cancellationToken = default);
}
