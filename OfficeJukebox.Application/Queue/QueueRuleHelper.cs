using OfficeJukebox.Application.Queue.Rules;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Queue;

public interface IQueueRuleHelper
{
    IEnumerable<string> CannotQueueTrack(Track track, TrackRef trackRef, string user);
}

public sealed class QueueRuleHelper(IEnumerable<IQueueRule> queueRules) : IQueueRuleHelper
{
    public IEnumerable<string> CannotQueueTrack(Track track, TrackRef trackRef, string user)
    {
        return queueRules.Select(q => q.CannotQueue(track, trackRef, user));
    }
}
