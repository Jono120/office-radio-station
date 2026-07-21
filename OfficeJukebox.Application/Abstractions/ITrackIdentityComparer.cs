using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Abstractions;

public interface ITrackIdentityComparer
{
    bool AreSame(TrackRef left, TrackRef right);
    TrackRef FromTrackPlay(TrackPlay trackPlay);
}

public sealed class TrackIdentityComparer : ITrackIdentityComparer
{
    public bool AreSame(TrackRef left, TrackRef right) =>
        !left.IsEmpty && !right.IsEmpty &&
        string.Equals(left.Provider, right.Provider, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.ExternalId, right.ExternalId, StringComparison.OrdinalIgnoreCase);

    public TrackRef FromTrackPlay(TrackPlay trackPlay) =>
        new(trackPlay.Provider, trackPlay.ExternalId);
}
