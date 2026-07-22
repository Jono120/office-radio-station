namespace OfficeJukebox.Contracts.Queue;

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

public sealed record PlaybackProgressEvent(
    Guid? TrackPlayId,
    int ProgressMs,
    int DurationMs,
    bool IsPlaying);
