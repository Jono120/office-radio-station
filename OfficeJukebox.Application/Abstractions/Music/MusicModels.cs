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

/// <summary>
/// A catalog search hit. Carries the provider's native external id alongside
/// the track so callers never have to re-derive the id from the track's link.
/// </summary>
public sealed record CatalogSearchResult(string ExternalId, Track Track);

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
