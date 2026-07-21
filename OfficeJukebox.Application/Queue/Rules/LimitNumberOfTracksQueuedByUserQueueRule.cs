using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Queue.Rules;

public sealed class LimitNumberOfTracksQueuedByUserQueueRule(
    IQueueManager queueManager,
    IOptions<QueueRulesOptions> options) : IQueueRule
{
    public string CannotQueue(Track track, TrackRef trackRef, string user)
    {
        var maxTracks = options.Value.MaxTracksPerUser;
        return queueManager.GetAll().Count(q => q.User == user) >= maxTracks
            ? $"You can only queue {maxTracks} tracks at a time."
            : string.Empty;
    }
}
