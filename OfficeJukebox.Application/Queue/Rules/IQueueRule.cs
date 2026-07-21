using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Queue.Rules;

public interface IQueueRule
{
    string CannotQueue(Track track, TrackRef trackRef, string user);
}
