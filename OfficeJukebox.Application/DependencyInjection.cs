using Microsoft.Extensions.DependencyInjection;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Configuration;
using OfficeJukebox.Application.Playback;
using OfficeJukebox.Application.Queue;
using OfficeJukebox.Application.Queue.Rules;
using OfficeJukebox.Application.Queue.Services;
using OfficeJukebox.Application.Scoring;
using OfficeJukebox.Application.Skip;
using OfficeJukebox.Application.Skip.Rules;
using OfficeJukebox.Application.Veto.Rules;

namespace OfficeJukebox.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.AddSingleton<ITrackIdentityComparer, TrackIdentityComparer>();
        services.AddSingleton<IQueueManager, QueueManager>();
        services.AddScoped<IQueueRuleHelper, QueueRuleHelper>();
        services.AddSingleton<ISkipHelper, SkipHelper>();
        services.AddScoped<ITrackScoreService, TrackScoreService>();
        services.AddScoped<IEnqueueTrackService, EnqueueTrackService>();
        services.AddSingleton<IGetQueueService, GetQueueService>();
        services.AddScoped<IQueueBootstrapService, QueueBootstrapService>();
        services.AddSingleton<IQueueNotifier, NullQueueNotifier>();

        services.AddSingleton<IQueueRule, CannotQueueTrackAlreadyPlayingQueueRule>();
        services.AddSingleton<IQueueRule, LimitNumberOfTracksQueuedByUserQueueRule>();
        services.AddScoped<IQueueRule, CannotQueueTrackThatHasPlayedInTheLastXHoursQueueRule>();
        services.AddScoped<IVetoRule, ExceededDailyLimitVetoRule>();
        services.AddSingleton<ISkipRule, DefaultSkipRule>();
        services.AddSingleton<ISkipRule, OutOfHoursSkipRule>();

        services.AddOptions<QueueRulesOptions>().BindConfiguration(QueueRulesOptions.SectionName);
        services.AddOptions<VetoRulesOptions>().BindConfiguration(VetoRulesOptions.SectionName);
        services.AddOptions<SkipRulesOptions>().BindConfiguration(SkipRulesOptions.SectionName);

        return services;
    }

    public static IServiceCollection AddPlayback(this IServiceCollection services)
    {
        services.AddSingleton<PlaybackOrchestrator>();
        services.AddSingleton<IPlaybackOrchestrator>(sp => sp.GetRequiredService<PlaybackOrchestrator>());
        services.AddSingleton<IMusicPlayer>(sp => sp.GetRequiredService<IPlaybackOrchestrator>());
        services.AddHostedService<PlaybackLoopService>();
        return services;
    }
}
