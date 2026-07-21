using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Application.Queue.Rules;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;
using OfficeJukebox.Domain.ValueObjects;
using Moq;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Tests.Queue.Rules;

public class CannotQueueTrackThatHasPlayedInTheLastXHoursTests
{
    private const string User = "jimmi";
    private readonly TrackPlay _trackPlay;
    private readonly TrackRef _trackRef = new("manual", "Test Track name");

    public CannotQueueTrackThatHasPlayedInTheLastXHoursTests()
    {
        _trackPlay = new TrackPlay
        {
            User = User,
            Provider = "manual",
            ExternalId = "Test Track name",
            Track = new Track { Name = "Test Track name", Album = new Album { Name = "Test Album name" } }
        };
    }

    [Fact]
    public void Track_played_over_repeat_window_can_be_queued_again()
    {
        _trackPlay.StartedAt = DateTime.Now.AddHours(-4).AddMinutes(-1);
        var repository = CreateRepository(_trackPlay);
        var rule = CreateRule(repository, hours: 4);

        var result = rule.CannotQueue(_trackPlay.Track, _trackRef, User);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Track_played_within_repeat_window_cannot_be_queued()
    {
        _trackPlay.StartedAt = DateTime.Now.AddHours(-3).AddMinutes(-59);
        var repository = CreateRepository(_trackPlay);
        var rule = CreateRule(repository, hours: 4);

        var result = rule.CannotQueue(_trackPlay.Track, _trackRef, User);

        Assert.NotEmpty(result);
    }

    private static CannotQueueTrackThatHasPlayedInTheLastXHoursQueueRule CreateRule(
        Mock<ITrackPlayRepository> repository,
        int hours)
    {
        var timeProvider = new Mock<ITimeProvider>();
        timeProvider.Setup(t => t.Now).Returns(DateTime.Now);
        return new CannotQueueTrackThatHasPlayedInTheLastXHoursQueueRule(
            repository.Object,
            new TrackIdentityComparer(),
            Options.Create(new QueueRulesOptions { DontRepeatHours = hours }),
            timeProvider.Object);
    }

    private static Mock<ITrackPlayRepository> CreateRepository(TrackPlay play)
    {
        var mock = new Mock<ITrackPlayRepository>();
        mock.Setup(r => r.GetAll()).Returns(new List<TrackPlay> { play }.AsQueryable());
        return mock;
    }
}
