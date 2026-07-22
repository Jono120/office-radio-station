using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;
using OfficeJukebox.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Queue.Rules;

public sealed class CannotQueueTrackThatHasPlayedInTheLastXHoursQueueRule(
    ITrackPlayRepository trackPlayRepository,
    ITrackIdentityComparer identityComparer,
    IOptions<QueueRulesOptions> options,
    ITimeProvider timeProvider) : IQueueRule
{
    public string CannotQueue(Track track, TrackRef trackRef, string user)
    {
        if (track is null || trackRef.IsEmpty)
        {
            return string.Empty;
        }

        var hours = options.Value.DontRepeatHours;
        // Duration-based window: pure UTC, no office-zone conversion needed
        // because StartedAt/QueuedAt are persisted in UTC.
        var cutoff = timeProvider.UtcNow.AddHours(-hours);

        // The rule is global: nobody may repeat a recently played track,
        // regardless of who queued it (per-user counts are handled by
        // LimitNumberOfTracksQueuedByUser). The cutoff filter runs in SQL;
        // only the small recent slice is identity-compared in memory.
        // QueuedAt covers plays that are queued but not yet started.
        var hasRecentPlay = trackPlayRepository.GetAll()
            .Where(q => (q.StartedAt ?? q.QueuedAt) > cutoff)
            .AsEnumerable()
            .Any(q => identityComparer.AreSame(identityComparer.FromTrackPlay(q), trackRef));

        return hasRecentPlay
            ? $"Cannot queue this track: it has already been played or queued in the last {hours} hours."
            : string.Empty;
    }
}
