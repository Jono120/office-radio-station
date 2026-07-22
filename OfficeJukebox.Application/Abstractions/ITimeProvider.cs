namespace OfficeJukebox.Application.Abstractions;

/// <summary>
/// Single source of time for the application.
/// Timestamps are always persisted in UTC; rules that reason about wall-clock
/// "time frames" (office hours, daily limits) use the admin-configured office
/// time zone via <see cref="OfficeTimeZone"/> / <see cref="OfficeNow"/>.
/// </summary>
public interface ITimeProvider
{
    DateTime UtcNow { get; }

    /// <summary>The office time zone configured under Organization:TimeZone (machine zone when unset).</summary>
    TimeZoneInfo OfficeTimeZone { get; }

    /// <summary>Current wall-clock time at the office location.</summary>
    DateTime OfficeNow { get; }
}
