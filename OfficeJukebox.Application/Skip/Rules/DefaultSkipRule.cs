using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Domain.Entities;
using Microsoft.Extensions.Options;

namespace OfficeJukebox.Application.Skip.Rules;

public sealed class DefaultSkipRule(IOptions<SkipRulesOptions> options) : ISkipRule
{
    public int GetRequiredVetoCount(TrackPlay track)
    {
        var minimumVetoCount = track.LikeCount;
        var configuredMinimum = options.Value.MinimumVetoCount;
        return configuredMinimum > minimumVetoCount ? configuredMinimum : minimumVetoCount;
    }
}
