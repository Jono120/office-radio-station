namespace OfficeJukebox.Contracts.Queue;

/// <summary>
/// What the web client sends to the Api. Deliberately has no User field —
/// identity comes from the caller's session, never the request body, so users
/// cannot impersonate each other.
/// </summary>
public sealed record QueueTrackClientRequest(
    string Provider,
    string ExternalId,
    string? TrackName,
    string? AlbumName,
    string? ExternalLink,
    string? Reason);

/// <summary>
/// The Api → Player wire shape. User is the session-derived canonical email,
/// filled in by the Api before proxying.
/// </summary>
public sealed record QueueTrackRequest(
    string User,
    string Provider,
    string ExternalId,
    string? TrackName,
    string? AlbumName,
    string? ExternalLink,
    string? Reason);

public sealed record QueueItemResponse(
    Guid Id,
    string User,
    string Provider,
    string ExternalId,
    string TrackName,
    string? AlbumName,
    string? ExternalLink,
    string? Reason,
    string Status);

// Api → Player wire shapes: the client sends no body for veto/skip; the Api
// fills User from the caller's session.
public sealed record VetoRequest(string User);

public sealed record SkipRequest(string User);

public sealed record NowPlayingResponse(
    Guid? Id,
    string? User,
    string? Provider,
    string? ExternalId,
    string? TrackName,
    string? AlbumName,
    string? ArtworkUrl,
    int? ProgressMs,
    int? DurationMs,
    bool IsPlaying,
    string? DeviceName);

public sealed record SearchResultItem(
    string Provider,
    string ExternalId,
    string Name,
    string? AlbumName,
    string? ArtworkUrl,
    long DurationMs,
    string? ExternalLink);

public sealed record ProviderInfoResponse(
    string Id,
    string DisplayName,
    bool Enabled,
    bool IsAuthenticated,
    string[] Capabilities);

public sealed record PlaybackDeviceResponse(
    string Id,
    string Name,
    string Provider,
    bool IsActive,
    string? Type);

public sealed record SetDeviceRequest(string Provider, string DeviceId);

public sealed record SaveProviderConnectionRequest(string ConnectionString);

public sealed record ProviderConnectUrlResponse(string Url);

/// <summary>
/// The single payload shape for playback progress, used end to end:
/// Player notifier → Api internal endpoint → SignalR "PlaybackProgress" event
/// (serialized camelCase: progressMs / durationMs / isPlaying).
/// </summary>
public sealed record PlaybackProgressEvent(
    int ProgressMs,
    int DurationMs,
    bool IsPlaying);
