using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Abstractions;

public interface IQueueManager
{
    void Enqueue(TrackPlay trackPlay);
    TrackPlay? Dequeue();
    bool Contains(Guid trackPlayId);
    TrackPlay? Get(Guid trackPlayId);
    IReadOnlyList<TrackPlay> GetAll();
}
