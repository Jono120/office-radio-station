using Microsoft.AspNetCore.Mvc;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Application.Queue.Services;
using OfficeJukebox.Contracts.Queue;

namespace OfficeJukebox.Player.Endpoints;

public static class QueueEndpoints
{
    public static void MapQueueEndpoints(this WebApplication app)
    {
        app.MapGet("/queue", (IGetQueueService queueService) => Results.Ok(queueService.GetQueue()));

        app.MapPost("/queue", async (
            QueueTrackRequest request,
            IEnqueueTrackService enqueueService,
            CancellationToken cancellationToken) =>
        {
            var (trackPlay, errors) = await enqueueService.EnqueueAsync(request, cancellationToken);
            if (errors.Count > 0)
            {
                return Results.BadRequest(new { errors });
            }

            return Results.Created($"/queue/{trackPlay!.Id}", new { trackPlay.Id });
        });

        app.MapGet("/now-playing", (IMusicPlayer musicPlayer) =>
        {
            var current = musicPlayer.CurrentlyPlayingTrack;
            if (current is null)
            {
                return Results.Ok(new NowPlayingResponse(null, null, null, null, null, null, null, null, null, false, null));
            }

            return Results.Ok(new NowPlayingResponse(
                current.Id,
                current.User,
                current.Provider,
                current.ExternalId,
                current.Track.Name,
                current.Track.Album?.Name,
                current.Track.TrackArtworkUrl,
                musicPlayer.CurrentPlaybackState?.ProgressMs,
                (int?)current.Track.DurationMilliseconds,
                musicPlayer.CurrentPlaybackState?.IsPlaying ?? false,
                musicPlayer.CurrentPlaybackState?.DeviceName));
        });

        app.MapPost("/queue/{id:guid}/veto", async (
            Guid id,
            VetoRequest request,
            IPlaybackOrchestrator orchestrator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return await orchestrator.VetoAsync(id, request.User, cancellationToken)
                    ? Results.Ok()
                    : Results.NotFound(new { error = "Track is not playing or queued." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/queue/{id:guid}/skip", async (
            Guid id,
            SkipRequest request,
            IPlaybackOrchestrator orchestrator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return await orchestrator.SkipAsync(id, request.User, cancellationToken)
                    ? Results.Ok()
                    : Results.NotFound(new { error = "Track is not playing or queued." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/playback/devices", async (IMusicProviderRegistry registry, [FromQuery] string provider, CancellationToken cancellationToken) =>
        {
            var playback = registry.GetPlayback(provider);
            if (playback is null)
            {
                return Results.NotFound();
            }

            var devices = await playback.ListDevicesAsync(cancellationToken);
            return Results.Ok(devices.Select(d => new PlaybackDeviceResponse(d.Id, d.Name, provider, d.IsActive, d.Type)));
        });

        app.MapPut("/playback/device", async (SetDeviceRequest request, IMusicProviderRegistry registry, CancellationToken cancellationToken) =>
        {
            var playback = registry.GetPlayback(request.Provider);
            if (playback is null)
            {
                return Results.NotFound();
            }

            await playback.SetActiveDeviceAsync(request.DeviceId, cancellationToken);
            return Results.Ok();
        });
    }
}
