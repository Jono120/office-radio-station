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

// No IsEnabled flag: disabled providers are simply not registered, so a
// ProviderInfo existing implies the provider is enabled.
public sealed record ProviderInfo(
    string Id,
    string DisplayName,
    ProviderCapabilities Capabilities);

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
