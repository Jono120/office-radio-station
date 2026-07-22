using Microsoft.AspNetCore.SignalR;

namespace OfficeJukebox.Api.Hubs;

/// <summary>
/// Connection point for web clients. Events (QueueChanged, NowPlayingChanged,
/// PlaybackProgress) are broadcast by InternalNotificationsController when the
/// Player reports a change; the hub itself defines no client-callable methods.
/// </summary>
public sealed class QueueHub : Hub;
