using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Skip.Rules;

public interface ISkipRule
{
    int GetRequiredVetoCount(TrackPlay track);
}
