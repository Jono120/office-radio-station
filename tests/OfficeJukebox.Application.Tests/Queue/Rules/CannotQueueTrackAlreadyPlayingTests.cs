using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Playback;
using OfficeJukebox.Application.Queue.Rules;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Tests.Queue.Rules;

public class CannotQueueTrackAlreadyPlayingTests
{
    private readonly TrackPlay _queuedTrack;
    private readonly TrackPlay _queuedTrack2;
    private readonly TrackPlay _queuedTrack3;
    private readonly TrackRef _trackRef1 = new("manual", "track-1");
    private readonly TrackRef _trackRef2 = new("manual", "track-2");

    public CannotQueueTrackAlreadyPlayingTests()
    {
        var playedTime = DateTime.Now;
        _queuedTrack = CreateTrackPlay("track-1", "Test Track name", "Test Album name", playedTime);
        _queuedTrack2 = CreateTrackPlay("track-2", "Test Track name2", "Test Album name2", playedTime);
        _queuedTrack3 = CreateTrackPlay("track-3", "Test Track name2", "Test Album name", playedTime);
    }

    [Fact]
    public void Can_queue_when_current_track_is_different()
    {
        var playbackState = new PlaybackRuntimeState();
        playbackState.SetCurrentTrack(_queuedTrack2);
        var rule = new CannotQueueTrackAlreadyPlayingQueueRule(playbackState, new TrackIdentityComparer());

        var result = rule.CannotQueue(_queuedTrack.Track, _trackRef1, "jimmi");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Cannot_queue_when_current_track_is_the_same()
    {
        var playbackState = new PlaybackRuntimeState();
        playbackState.SetCurrentTrack(_queuedTrack);
        var rule = new CannotQueueTrackAlreadyPlayingQueueRule(playbackState, new TrackIdentityComparer());

        var result = rule.CannotQueue(_queuedTrack.Track, _trackRef1, "jimmi");

        Assert.NotEqual(string.Empty, result);
    }

    [Fact]
    public void Can_queue_when_same_album_but_different_track_name()
    {
        var playbackState = new PlaybackRuntimeState();
        playbackState.SetCurrentTrack(_queuedTrack3);
        var rule = new CannotQueueTrackAlreadyPlayingQueueRule(playbackState, new TrackIdentityComparer());

        var result = rule.CannotQueue(_queuedTrack.Track, _trackRef1, "jimmi");

        Assert.Equal(string.Empty, result);
    }

    private static TrackPlay CreateTrackPlay(string externalId, string trackName, string albumName, DateTime playedTime) =>
        new()
        {
            StartedAt = playedTime,
            User = "Test User",
            Provider = "manual",
            ExternalId = externalId,
            Track = new Track
            {
                Name = trackName,
                Album = new Album { Name = albumName }
            }
        };
}
