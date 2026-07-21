using OfficeJukebox.Domain.Entities;

using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Abstractions.Music;

[Flags]
public enum ProviderCapabilities
{
    None = 0,
    Search = 1,
    Resolve = 2,
    DevicePlayback = 4,
    RequiresAuth = 8
}

public sealed record ProviderInfo(
    string Id,
    string DisplayName,
    ProviderCapabilities Capabilities,
    bool IsEnabled);

public sealed record PlaybackDevice(
    string Id,
    string Name,
    bool IsActive,
    string? Type);

public sealed record PlaybackState(
    bool IsPlaying,
    int ProgressMs,
    int DurationMs,
    TrackRef? CurrentTrack,
    string? DeviceName,
    string? DeviceId);
