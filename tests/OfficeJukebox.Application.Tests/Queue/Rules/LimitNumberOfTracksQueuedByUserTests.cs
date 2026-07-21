using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Application.Queue.Rules;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;
using Moq;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Tests.Queue.Rules;

public class LimitNumberOfTracksQueuedByUserTests
{
    private const string User = "jimmi";
    private readonly List<TrackPlay> _tracks;

    public LimitNumberOfTracksQueuedByUserTests()
    {
        _tracks = Enumerable.Range(1, 5)
            .Select(i => new TrackPlay
            {
                User = User,
                Track = new Track { Name = $"Track {i}", Album = new Album { Name = $"Album {i}" } }
            })
            .ToList();
    }

    [Fact]
    public void Can_queue_with_one_song_currently_queued()
    {
        var queueManager = CreateQueueManager(_tracks.Take(1));
        var rule = CreateRule(queueManager, 5);

        var result = rule.CannotQueue(_tracks[0].Track, new TrackRef("manual", "new"), User);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Can_queue_with_four_songs_currently_queued()
    {
        var queueManager = CreateQueueManager(_tracks.Take(4));
        var rule = CreateRule(queueManager, 5);

        var result = rule.CannotQueue(_tracks[0].Track, new TrackRef("manual", "new"), User);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Cannot_queue_with_five_songs_currently_queued()
    {
        var queueManager = CreateQueueManager(_tracks);
        var rule = CreateRule(queueManager, 5);

        var result = rule.CannotQueue(_tracks[0].Track, new TrackRef("manual", "new"), User);

        Assert.NotEqual(string.Empty, result);
    }

    private static LimitNumberOfTracksQueuedByUserQueueRule CreateRule(Mock<IQueueManager> queueManager, int max) =>
        new(queueManager.Object, Options.Create(new QueueRulesOptions { MaxTracksPerUser = max }));

    private static Mock<IQueueManager> CreateQueueManager(IEnumerable<TrackPlay> items)
    {
        var mock = new Mock<IQueueManager>();
        mock.Setup(q => q.GetAll()).Returns(items.ToList());
        return mock;
    }
}
