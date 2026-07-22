using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Skip.Rules;

public sealed class OutOfHoursSkipRule(ITimeProvider timeProvider) : ISkipRule
{
    private const int RequiredVetoCountOutOfHours = 1;

    public int GetRequiredVetoCount(TrackPlay track)
    {
        return IsOutOfHours() ? RequiredVetoCountOutOfHours : int.MaxValue;
    }

    private bool IsOutOfHours()
    {
        // Office hours are wall-clock at the admin-configured office location
        // (Organization:TimeZone), not the server's zone and not UTC.
        var now = timeProvider.OfficeNow;
        return now.Hour >= 18
               || now.Hour < 8
               || now.DayOfWeek == DayOfWeek.Saturday
               || now.DayOfWeek == DayOfWeek.Sunday;
    }
}
