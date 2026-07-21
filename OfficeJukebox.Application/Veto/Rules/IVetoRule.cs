using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Application.Veto.Rules;

public interface IVetoRule
{
    bool CantVetoTrack(string vetoedByUser, TrackPlay track);
}
