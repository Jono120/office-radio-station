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
    ILogger<PlaybackOrchestrator> logger) : IPlaybackOrchestrator
{
    public TrackPlay? CurrentlyPlayingTrack => runtimeState.CurrentlyPlayingTrack;

    public PlaybackState? CurrentPlaybackState => runtimeState.CurrentPlaybackState;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await PlayNextIfIdleAsync(cancellationToken);
    }

    public async Task SkipCurrentAsync(string user, CancellationToken cancellationToken = default)
    {
        var current = runtimeState.GetCurrentTrack();
        if (current is null)
        {
            return;
        }

        if (current.VetoCount < skipHelper.RequiredVetoCount(current))
        {
            throw new InvalidOperationException("Skip threshold not met.");
        }

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
        await PlayNextIfIdleAsync(cancellationToken);
    }

    public async Task VetoCurrentAsync(string user, CancellationToken cancellationToken = default)
    {
        var current = runtimeState.GetCurrentTrack();
        if (current is null)
        {
            return;
        }

        foreach (var rule in vetoRules)
        {
            if (rule.CantVetoTrack(user, current))
            {
                throw new InvalidOperationException("Daily veto limit exceeded.");
            }
        }

        current.Vetoes.Add(new TrackPlayVeto { ByUser = user, TrackPlayId = current.Id });
        await trackPlayRepository.UpdateAsync(current, cancellationToken);
        await trackPlayRepository.SaveChangesAsync(cancellationToken);
        await queueNotifier.NotifyNowPlayingChangedAsync(cancellationToken);
    }

    public async Task PollAndAdvanceAsync(CancellationToken cancellationToken = default)
    {
        var current = runtimeState.GetCurrentTrack();
        if (current is null)
        {
            await PlayNextIfIdleAsync(cancellationToken);
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
            await PlayNextIfIdleAsync(cancellationToken);
        }
        else if (!state.IsPlaying && state.DurationMs > 0 && state.ProgressMs >= state.DurationMs - 2000)
        {
            await CompleteCurrentAsync(current, cancellationToken);
            await PlayNextIfIdleAsync(cancellationToken);
        }
    }

    private async Task PlayNextIfIdleAsync(CancellationToken cancellationToken)
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
        next.StartedAt = DateTime.UtcNow;
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
