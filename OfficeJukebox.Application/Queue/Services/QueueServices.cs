using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Application.Queue;
using OfficeJukebox.Application.Queue.Rules;
using OfficeJukebox.Application.Serialization;
using OfficeJukebox.Contracts.Queue;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Queue.Services;

public interface IEnqueueTrackService
{
    Task<(TrackPlay? TrackPlay, IReadOnlyList<string> Errors)> EnqueueAsync(
        QueueTrackRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class EnqueueTrackService(
    IQueueManager queueManager,
    IQueueRuleHelper queueRuleHelper,
    IMusicProviderRegistry providerRegistry,
    ITrackPlayRepository trackPlayRepository,
    IQueueNotifier queueNotifier,
    ITimeProvider timeProvider) : IEnqueueTrackService
{
    public async Task<(TrackPlay? TrackPlay, IReadOnlyList<string> Errors)> EnqueueAsync(
        QueueTrackRequest request,
        CancellationToken cancellationToken = default)
    {
        var providerId = string.IsNullOrWhiteSpace(request.Provider) ? "manual" : request.Provider;
        var externalId = request.ExternalId;
        if (string.IsNullOrWhiteSpace(externalId) && !string.IsNullOrWhiteSpace(request.TrackName))
        {
            externalId = request.TrackName;
        }

        var trackRef = new TrackRef(providerId, externalId ?? string.Empty);
        var catalog = providerRegistry.GetCatalog(providerId);
        Track track;

        if (catalog is not null && catalog.Capabilities.HasFlag(ProviderCapabilities.Resolve))
        {
            track = await catalog.ResolveAsync(trackRef, cancellationToken);
        }
        else
        {
            track = new Track
            {
                Name = request.TrackName ?? externalId ?? string.Empty,
                Link = request.ExternalLink ?? string.Empty,
                Album = new Album { Name = request.AlbumName ?? string.Empty }
            };
        }

        var errors = queueRuleHelper.CannotQueueTrack(track, trackRef, request.User)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        var trackPlay = new TrackPlay
        {
            User = request.User,
            QueuedAt = timeProvider.UtcNow,
            Track = track,
            Provider = providerId,
            ExternalId = externalId ?? string.Empty,
            ExternalLink = request.ExternalLink ?? track.Link,
            Reason = request.Reason,
            Status = TrackPlayStatus.Queued,
            StartedAt = null
        };
        TrackJsonSerializer.SyncTrackJson(trackPlay);

        await trackPlayRepository.AddAsync(trackPlay, cancellationToken);
        await trackPlayRepository.SaveChangesAsync(cancellationToken);
        queueManager.Enqueue(trackPlay);
        await queueNotifier.NotifyQueueChangedAsync(cancellationToken);

        return (trackPlay, Array.Empty<string>());
    }
}

public interface IGetQueueService
{
    IReadOnlyList<QueueItemResponse> GetQueue();
}

public sealed class GetQueueService(IQueueManager queueManager) : IGetQueueService
{
    public IReadOnlyList<QueueItemResponse> GetQueue() =>
        queueManager.GetAll()
            .Select(t => new QueueItemResponse(
                t.Id,
                t.User,
                t.Provider,
                t.ExternalId,
                t.Track.Name,
                t.Track.Album?.Name,
                t.ExternalLink,
                t.Reason,
                t.Status.ToString()))
            .ToList();
}

public interface IQueueBootstrapService
{
    Task LoadQueuedTracksAsync(CancellationToken cancellationToken = default);
}

public sealed class QueueBootstrapService(
    ITrackPlayRepository trackPlayRepository,
    IQueueManager queueManager) : IQueueBootstrapService
{
    public async Task LoadQueuedTracksAsync(CancellationToken cancellationToken = default)
    {
        // Crash recovery: a track that was mid-play when the Player stopped is
        // stranded in Playing status. Re-queue it ahead of everything else so
        // it isn't silently lost on restart.
        var stranded = await trackPlayRepository.GetByStatusAsync(TrackPlayStatus.Playing, cancellationToken);
        foreach (var trackPlay in stranded)
        {
            trackPlay.Status = TrackPlayStatus.Queued;
            trackPlay.StartedAt = null;
            await trackPlayRepository.UpdateAsync(trackPlay, cancellationToken);
        }

        if (stranded.Count > 0)
        {
            await trackPlayRepository.SaveChangesAsync(cancellationToken);
        }

        var queued = await trackPlayRepository.GetByStatusAsync(TrackPlayStatus.Queued, cancellationToken);
        foreach (var trackPlay in queued.OrderBy(t => t.Id))
        {
            TrackJsonSerializer.HydrateTrack(trackPlay);
            queueManager.Enqueue(trackPlay);
        }
    }
}
