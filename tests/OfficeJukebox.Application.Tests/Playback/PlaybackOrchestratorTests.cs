using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Application.Playback;
using OfficeJukebox.Application.Queue;
using OfficeJukebox.Application.Scoring;
using OfficeJukebox.Application.Skip;
using OfficeJukebox.Application.Veto.Rules;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;

namespace OfficeJukebox.Application.Tests.Playback;

public class PlaybackOrchestratorTests
{
    private readonly PlaybackRuntimeState _runtimeState = new();
    private readonly QueueManager _queueManager = new(NullLogger<QueueManager>.Instance);
    private readonly Mock<ITrackPlayRepository> _repository = new();
    private readonly Mock<ISkipHelper> _skipHelper = new();

    public PlaybackOrchestratorTests()
    {
        _repository.Setup(r => r.GetAll()).Returns(Array.Empty<TrackPlay>().AsQueryable());
    }

    [Fact]
    public async Task Veto_returns_false_for_unknown_track_id()
    {
        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.VetoAsync(Guid.NewGuid(), "a user");

        Assert.False(result);
    }

    [Fact]
    public async Task Skip_returns_false_for_unknown_track_id()
    {
        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.SkipAsync(Guid.NewGuid(), "a user");

        Assert.False(result);
    }

    [Fact]
    public async Task Veto_targets_the_queued_item_not_the_current_track()
    {
        var current = NewTrack("current");
        var queued = NewTrack("queued");
        _runtimeState.SetCurrentTrack(current);
        _queueManager.Enqueue(queued);

        var orchestrator = CreateOrchestrator();
        var result = await orchestrator.VetoAsync(queued.Id, "a user");

        Assert.True(result);
        Assert.Single(queued.Vetoes);
        Assert.Empty(current.Vetoes);
    }

    [Fact]
    public async Task Skip_of_queued_item_marks_it_skipped_and_leaves_current_playing()
    {
        var current = NewTrack("current");
        var queued = NewTrack("queued");
        queued.Vetoes.Add(new TrackPlayVeto { ByUser = "a user" });
        _runtimeState.SetCurrentTrack(current);
        _queueManager.Enqueue(queued);
        _skipHelper.Setup(s => s.RequiredVetoCount(It.IsAny<TrackPlay>())).Returns(1);

        var orchestrator = CreateOrchestrator();
        var result = await orchestrator.SkipAsync(queued.Id, "a user");

        Assert.True(result);
        Assert.Equal(TrackPlayStatus.Skipped, queued.Status);
        Assert.Same(current, _runtimeState.GetCurrentTrack());
        Assert.Null(_queueManager.Dequeue()); // skipped items are dropped
    }

    [Fact]
    public async Task Skip_below_threshold_throws()
    {
        var current = NewTrack("current");
        _runtimeState.SetCurrentTrack(current);
        _skipHelper.Setup(s => s.RequiredVetoCount(It.IsAny<TrackPlay>())).Returns(3);

        var orchestrator = CreateOrchestrator();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.SkipAsync(current.Id, "a user"));
    }

    [Fact]
    public async Task Concurrent_start_calls_dequeue_exactly_one_track()
    {
        _queueManager.Enqueue(NewTrack("first"));
        _queueManager.Enqueue(NewTrack("second"));

        // All callers share the runtime state (singleton) but get their own
        // orchestrator instance, mirroring the scoped registration in the app.
        var tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => CreateOrchestrator().StartAsync()))
            .ToArray();
        await Task.WhenAll(tasks);

        // Without the runtime-state lock, several callers pass the idle check
        // and each dequeues a different track, losing tracks. With it, exactly
        // one track starts playing and exactly one remains queued.
        Assert.NotNull(_runtimeState.GetCurrentTrack());
        Assert.Single(_queueManager.GetAll());
    }

    private static TrackPlay NewTrack(string name) =>
        new()
        {
            User = "someone",
            Provider = "manual",
            ExternalId = name,
            Track = new Track { Name = name },
            Status = TrackPlayStatus.Queued
        };

    private PlaybackOrchestrator CreateOrchestrator()
    {
        var registry = new Mock<IMusicProviderRegistry>();
        registry.Setup(r => r.GetPlayback(It.IsAny<string>())).Returns((IMusicPlaybackController?)null);

        var timeProvider = new Mock<ITimeProvider>();
        timeProvider.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);

        return new PlaybackOrchestrator(
            _runtimeState,
            _queueManager,
            registry.Object,
            _repository.Object,
            Mock.Of<ITrackScoreService>(),
            new NullQueueNotifier(),
            _skipHelper.Object,
            Array.Empty<IVetoRule>(),
            timeProvider.Object,
            NullLogger<PlaybackOrchestrator>.Instance);
    }
}
