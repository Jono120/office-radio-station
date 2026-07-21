using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Queue.Rules;

public sealed class CannotQueueTrackAlreadyPlayingQueueRule(
    IMusicPlayer musicPlayer,
    ITrackIdentityComparer identityComparer) : IQueueRule
{
    public string CannotQueue(Track track, TrackRef trackRef, string user)
    {
        if (track is null || trackRef.IsEmpty)
        {
            return string.Empty;
        }

        var current = musicPlayer.CurrentlyPlayingTrack;
        if (current is null)
        {
            return string.Empty;
        }

        var currentRef = identityComparer.FromTrackPlay(current);
        return identityComparer.AreSame(currentRef, trackRef)
            ? "Cannot queue this track as it is already playing."
            : string.Empty;
    }
}
