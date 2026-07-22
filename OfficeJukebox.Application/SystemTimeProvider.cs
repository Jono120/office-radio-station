using Microsoft.Extensions.Options;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;

namespace OfficeJukebox.Application;

public sealed class SystemTimeProvider : ITimeProvider
{
    public SystemTimeProvider(IOptions<OrganizationOptions> options)
    {
        var configured = options.Value.TimeZone;
        // Fail fast on an invalid id so a typo in Organization:TimeZone is caught
        // at startup instead of silently evaluating rules in the wrong zone.
        OfficeTimeZone = string.IsNullOrWhiteSpace(configured)
            ? TimeZoneInfo.Local
            : TimeZoneInfo.FindSystemTimeZoneById(configured);
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public TimeZoneInfo OfficeTimeZone { get; }

    public DateTime OfficeNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, OfficeTimeZone);
}
