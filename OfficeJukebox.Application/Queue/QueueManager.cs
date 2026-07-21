using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Queue;

public sealed class QueueManager : IQueueManager
{
    private readonly ConcurrentQueue<TrackPlay> _queue = new();
    private readonly ILogger<QueueManager> _logger;

    public QueueManager(ILogger<QueueManager> logger)
    {
        _logger = logger;
    }

    public void Enqueue(TrackPlay trackPlay)
    {
        _queue.Enqueue(trackPlay);
    }

    public TrackPlay? Dequeue()
    {
        if (!_queue.TryDequeue(out var dequeued))
        {
            return null;
        }

        if (dequeued.IsSkipped)
        {
            _logger.LogDebug("Skipping track {TrackName} (vetoed or admin skipped)", dequeued.Track.Name);
            return Dequeue();
        }

        return dequeued;
    }

    public bool Contains(Guid trackPlayId) => _queue.Any(t => t.Id == trackPlayId);

    public TrackPlay? Get(Guid trackPlayId) => _queue.FirstOrDefault(t => t.Id == trackPlayId);

    public IReadOnlyList<TrackPlay> GetAll() => _queue.ToArray();
}
