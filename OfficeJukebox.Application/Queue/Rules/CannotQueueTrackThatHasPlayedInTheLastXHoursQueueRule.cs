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
        var cutoff = timeProvider.Now.AddHours(-hours);
        var hasRecentPlay = trackPlayRepository.GetAll()
            .AsEnumerable()
            .Any(q =>
            {
                var playedRef = identityComparer.FromTrackPlay(q);
                return identityComparer.AreSame(playedRef, trackRef) &&
                       q.StartedAt > cutoff &&
                       q.User == user;
            });

        return hasRecentPlay
            ? $"Cannot queue a track that you queued in the last {hours} hours."
            : string.Empty;
    }
}
