using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Application.Queue;
using OfficeJukebox.Application.Serialization;
using OfficeJukebox.Application.Scoring;
using OfficeJukebox.Application.Skip;
using OfficeJukebox.Application.Veto.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Playback;

public sealed class PlaybackOrchestrator(
    PlaybackRuntimeState runtimeState,
    IQueueManager queueManager,
    IMusicProviderRegistry providerRegistry,
    ITrackPlayRepository trackPlayRepository,
    ITrackScoreService trackScoreService,
    IQueueNotifier queueNotifier,
    ISkipHelper skipHelper,
    IEnumerable<IVetoRule> vetoRules,
    ITimeProvider timeProvider,
    ILogger<PlaybackOrchestrator> logger) : IPlaybackOrchestrator
{
    public TrackPlay? CurrentlyPlayingTrack => runtimeState.CurrentlyPlayingTrack;

    public PlaybackState? CurrentPlaybackState => runtimeState.CurrentPlaybackState;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using var _ = await runtimeState.LockAsync(cancellationToken);
        await PlayNextIfIdleCoreAsync(cancellationToken);
    }

    public async Task<bool> SkipAsync(Guid trackPlayId, string user, CancellationToken cancellationToken = default)
    {
        using var _ = await runtimeState.LockAsync(cancellationToken);

        var current = runtimeState.GetCurrentTrack();
        if (current is not null && current.Id == trackPlayId)
        {
            EnsureSkipThresholdMet(current);

            current.IsSkipped = true;
            current.Status = TrackPlayStatus.Skipped;
            await trackPlayRepository.UpdateAsync(current, cancellationToken);
            await trackPlayRepository.SaveChangesAsync(cancellationToken);

            var playback = providerRegistry.GetPlayback(current.Provider);
            if (playback?.Capabilities.HasFlag(ProviderCapabilities.DevicePlayback) == true)
            {
                await playback.SkipAsync(cancellationToken);
            }

            runtimeState.SetCurrentTrack(null);
            await queueNotifier.NotifyNowPlayingChangedAsync(cancellationToken);
            await PlayNextIfIdleCoreAsync(cancellationToken);
            return true;
        }

        var queued = queueManager.Get(trackPlayId);
        if (queued is null)
        {
            return false;
        }

        EnsureSkipThresholdMet(queued);

        // Queued items never touched provider playback; marking them skipped is
        // enough — QueueManager.Dequeue drops IsSkipped entries.
        queued.IsSkipped = true;
        queued.Status = TrackPlayStatus.Skipped;
        await trackPlayRepository.UpdateAsync(queued, cancellationToken);
        await trackPlayRepository.SaveChangesAsync(cancellationToken);
        await queueNotifier.NotifyQueueChangedAsync(cancellationToken);
        return true;
    }

    public async Task<bool> VetoAsync(Guid trackPlayId, string user, CancellationToken cancellationToken = default)
    {
        using var _ = await runtimeState.LockAsync(cancellationToken);

        var current = runtimeState.GetCurrentTrack();
        var target = current is not null && current.Id == trackPlayId
            ? current
            : queueManager.Get(trackPlayId);

        if (target is null)
        {
            return false;
        }

        foreach (var rule in vetoRules)
        {
            if (rule.CantVetoTrack(user, target))
            {
                throw new InvalidOperationException("Daily veto limit exceeded.");
            }
        }

        var veto = new TrackPlayVeto { ByUser = user, TrackPlayId = target.Id };
        target.Vetoes.Add(veto); // keep the in-memory graph current for threshold checks
        await trackPlayRepository.AddVetoAsync(veto, cancellationToken);
        await trackPlayRepository.SaveChangesAsync(cancellationToken);

        if (ReferenceEquals(target, current))
        {
            await queueNotifier.NotifyNowPlayingChangedAsync(cancellationToken);
        }
        else
        {
            await queueNotifier.NotifyQueueChangedAsync(cancellationToken);
        }

        return true;
    }

    public async Task PollAndAdvanceAsync(CancellationToken cancellationToken = default)
    {
        using var _ = await runtimeState.LockAsync(cancellationToken);

        var current = runtimeState.GetCurrentTrack();
        if (current is null)
        {
            await PlayNextIfIdleCoreAsync(cancellationToken);
            return;
        }

        var playback = providerRegistry.GetPlayback(current.Provider);
        if (playback is null)
        {
            return;
        }

        var state = await playback.GetStateAsync(cancellationToken);
        runtimeState.SetPlaybackState(state);
        await queueNotifier.NotifyPlaybackProgressAsync(state.ProgressMs, state.DurationMs, state.IsPlaying, cancellationToken);

        if (!state.IsPlaying && state.ProgressMs == 0 && state.CurrentTrack is null)
        {
            await CompleteCurrentAsync(current, cancellationToken);
            await PlayNextIfIdleCoreAsync(cancellationToken);
        }
        else if (!state.IsPlaying && state.DurationMs > 0 && state.ProgressMs >= state.DurationMs - 2000)
        {
            await CompleteCurrentAsync(current, cancellationToken);
            await PlayNextIfIdleCoreAsync(cancellationToken);
        }
    }

    private void EnsureSkipThresholdMet(TrackPlay track)
    {
        if (track.VetoCount < skipHelper.RequiredVetoCount(track))
        {
            throw new InvalidOperationException("Skip threshold not met.");
        }
    }

    /// <summary>Must be called while holding the runtime-state lock.</summary>
    private async Task PlayNextIfIdleCoreAsync(CancellationToken cancellationToken)
    {
        if (runtimeState.GetCurrentTrack() is not null)
        {
            return;
        }

        var next = queueManager.Dequeue();
        if (next is null)
        {
            return;
        }

        TrackJsonSerializer.HydrateTrack(next);
        next.Status = TrackPlayStatus.Playing;
        next.StartedAt = timeProvider.UtcNow;
        await trackPlayRepository.UpdateAsync(next, cancellationToken);
        await trackPlayRepository.SaveChangesAsync(cancellationToken);

        var trackRef = new TrackRef(next.Provider, next.ExternalId);
        var playback = providerRegistry.GetPlayback(next.Provider);
        if (playback?.Capabilities.HasFlag(ProviderCapabilities.DevicePlayback) == true)
        {
            try
            {
                await playback.PlayAsync(trackRef, cancellationToken);
                runtimeState.SetPlaybackState(await playback.GetStateAsync(cancellationToken));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Playback failed for {Provider}:{ExternalId}", next.Provider, next.ExternalId);
            }
        }

        runtimeState.SetCurrentTrack(next);
        await queueNotifier.NotifyQueueChangedAsync(cancellationToken);
        await queueNotifier.NotifyNowPlayingChangedAsync(cancellationToken);
    }

    private async Task CompleteCurrentAsync(TrackPlay current, CancellationToken cancellationToken)
    {
        current.Status = TrackPlayStatus.Completed;
        await trackPlayRepository.UpdateAsync(current, cancellationToken);
        await trackPlayRepository.SaveChangesAsync(cancellationToken);
        await trackScoreService.ComputeScoresAsync(cancellationToken);
        runtimeState.SetCurrentTrack(null);
        await queueNotifier.NotifyNowPlayingChangedAsync(cancellationToken);
    }
}

public sealed class PlaybackLoopService(
    IServiceScopeFactory scopeFactory,
    ILogger<PlaybackLoopService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunScopedAsync(orchestrator => orchestrator.StartAsync(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunScopedAsync(orchestrator => orchestrator.PollAndAdvanceAsync(stoppingToken), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Playback loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task RunScopedAsync(
        Func<PlaybackOrchestrator, Task> action,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<PlaybackOrchestrator>();
        await action(orchestrator);
    }
}
