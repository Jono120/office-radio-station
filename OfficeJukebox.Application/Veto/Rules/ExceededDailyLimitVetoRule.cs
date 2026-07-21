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
        var vetoCount = trackPlayRepository.GetAll()
            .Count(q => q.StartedAt > timeProvider.Now.Date &&
                        q.Vetoes.Any(v => v.ByUser == vetoedByUser));

        return vetoCount >= dailyLimit;
    }
}
