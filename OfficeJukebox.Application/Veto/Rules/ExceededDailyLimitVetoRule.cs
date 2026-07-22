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

        var vetoCount = trackPlayRepository.GetAll()
            .Count(q => q.StartedAt >= dayStartUtc &&
                        q.Vetoes.Any(v => v.ByUser == vetoedByUser));

        return vetoCount >= dailyLimit;
    }
}
