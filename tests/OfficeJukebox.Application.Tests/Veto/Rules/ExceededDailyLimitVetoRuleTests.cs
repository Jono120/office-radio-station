using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Application.Veto.Rules;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;
using Moq;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Tests.Veto.Rules;

public class ExceededDailyLimitVetoRuleTests
{
    private readonly DateTime _today = new(2013, 10, 2, 10, 0, 0);

    [Fact]
    public void Returns_true_when_user_has_reached_daily_limit()
    {
        var repository = CreateRepository(
            Play("a user", new DateTime(2013, 10, 2, 9, 0, 0)),
            Play("a user", new DateTime(2013, 10, 2, 8, 0, 0)));

        var rule = CreateRule(repository, dailyLimit: 2);

        Assert.True(rule.CantVetoTrack("a user", new TrackPlay()));
    }

    [Fact]
    public void Returns_false_when_user_is_one_below_limit()
    {
        var repository = CreateRepository(Play("a user", new DateTime(2013, 10, 2, 9, 0, 0)));
        var rule = CreateRule(repository, dailyLimit: 2);

        Assert.False(rule.CantVetoTrack("a user", new TrackPlay()));
    }

    [Fact]
    public void Returns_false_when_vetoes_were_on_previous_day()
    {
        var repository = CreateRepository(
            Play("a user", new DateTime(2013, 10, 1, 9, 0, 0)),
            Play("a user", new DateTime(2013, 10, 1, 8, 0, 0)));
        var rule = CreateRule(repository, dailyLimit: 2);

        Assert.False(rule.CantVetoTrack("a user", new TrackPlay()));
    }

    [Fact]
    public void Returns_false_when_user_has_not_vetoed()
    {
        var repository = CreateRepository(
            Play("another user", new DateTime(2013, 10, 2, 9, 0, 0)),
            Play("another user", new DateTime(2013, 10, 2, 9, 0, 0)));
        var rule = CreateRule(repository, dailyLimit: 2);

        Assert.False(rule.CantVetoTrack("a user", new TrackPlay()));
    }

    private ExceededDailyLimitVetoRule CreateRule(Mock<ITrackPlayRepository> repository, int dailyLimit)
    {
        // Office zone pinned to UTC so office-local dates equal the UTC
        // StartedAt values used in the test fixtures.
        var timeProvider = new Mock<ITimeProvider>();
        timeProvider.Setup(t => t.OfficeNow).Returns(_today);
        timeProvider.Setup(t => t.OfficeTimeZone).Returns(TimeZoneInfo.Utc);
        return new ExceededDailyLimitVetoRule(
            repository.Object,
            Options.Create(new VetoRulesOptions { DailyLimit = dailyLimit }),
            timeProvider.Object);
    }

    private static Mock<ITrackPlayRepository> CreateRepository(params TrackPlay[] plays)
    {
        var mock = new Mock<ITrackPlayRepository>();
        mock.Setup(r => r.GetAll()).Returns(plays.AsQueryable());
        return mock;
    }

    private static TrackPlay Play(string user, DateTime startedAt) =>
        new()
        {
            StartedAt = startedAt,
            Vetoes = new List<TrackPlayVeto> { new() { ByUser = user } }
        };
}
