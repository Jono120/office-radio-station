using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Skip.Rules;
using OfficeJukebox.Domain.Entities;
using Moq;

namespace OfficeJukebox.Application.Tests.Skip.Rules;

public class OutOfHoursSkipRuleTests
{
    [Theory]
    [InlineData(2013, 4, 10, 18, 0, 1)]
    [InlineData(2013, 4, 10, 18, 0, 0)]
    [InlineData(2013, 4, 10, 7, 59, 59)]
    [InlineData(2013, 4, 10, 0, 0, 0)]
    public void Returns_one_outside_business_hours(int year, int month, int day, int hour, int minute, int second)
    {
        var rule = CreateRule(new DateTime(year, month, day, hour, minute, second));
        var result = rule.GetRequiredVetoCount(new TrackPlay());
        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData(2013, 4, 10, 8, 0, 1)]
    [InlineData(2013, 4, 10, 8, 0, 0)]
    [InlineData(2013, 4, 10, 12, 0, 0)]
    public void Returns_int_max_during_business_hours_on_weekday(int year, int month, int day, int hour, int minute, int second)
    {
        var rule = CreateRule(new DateTime(year, month, day, hour, minute, second));
        var result = rule.GetRequiredVetoCount(new TrackPlay());
        Assert.Equal(int.MaxValue, result);
    }

    [Theory]
    [InlineData(2013, 12, 7, 12, 0, 0)]
    [InlineData(2013, 12, 8, 12, 0, 0)]
    public void Returns_one_on_weekend(int year, int month, int day, int hour, int minute, int second)
    {
        var rule = CreateRule(new DateTime(year, month, day, hour, minute, second));
        var result = rule.GetRequiredVetoCount(new TrackPlay());
        Assert.Equal(1, result);
    }

    private static OutOfHoursSkipRule CreateRule(DateTime now)
    {
        // Office hours are evaluated against OfficeNow (the admin-configured
        // office wall clock), so that's what the test controls.
        var timeProvider = new Mock<ITimeProvider>();
        timeProvider.Setup(t => t.OfficeNow).Returns(now);
        return new OutOfHoursSkipRule(timeProvider.Object);
    }
}
