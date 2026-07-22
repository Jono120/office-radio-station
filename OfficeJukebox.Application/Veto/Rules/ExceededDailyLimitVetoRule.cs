using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Veto.Rules;

public sealed class ExceededDailyLimitVetoRule(
    ITrackPlayRepository trackPlayRepository,
    IOptions<VetoRulesOptions> options,
    ITimeProvider timeProvider) : IVetoRule
{
    public bool CantVetoTrack(string vetoedByUser, TrackPlay track)
    {
        var dailyLimit = options.Value.DailyLimit;

        // "Today" is the office-local day (Organization:TimeZone). Compute its
        // start as a UTC instant so the comparison against UTC StartedAt stays
        // translatable to SQL.
        var officeToday = timeProvider.OfficeNow.Date;
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(officeToday, DateTimeKind.Unspecified),
            timeProvider.OfficeTimeZone);

        // Count by when the veto was cast (VetoedAt), not when the vetoed
        // track started playing — a veto cast today on yesterday's track
        // still consumes today's allowance.
        var vetoCount = trackPlayRepository.GetAll()
            .SelectMany(q => q.Vetoes)
            .Count(v => v.ByUser == vetoedByUser && v.VetoedAt >= dayStartUtc);

        return vetoCount >= dailyLimit;
    }
}
