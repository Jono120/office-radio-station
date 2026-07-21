using OfficeJukebox.Application.Skip.Rules;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Skip;

public interface ISkipHelper
{
    int RequiredVetoCount(TrackPlay track);
}

public sealed class SkipHelper(IEnumerable<ISkipRule> skipRules) : ISkipHelper
{
    public int RequiredVetoCount(TrackPlay track)
    {
        return skipRules.Min(rule => rule.GetRequiredVetoCount(track));
    }
}
