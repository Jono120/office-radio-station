using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Infrastructure.Music;
using OfficeJukebox.Infrastructure.Music.AppleMusic;
using OfficeJukebox.Infrastructure.Music.Auth;
using OfficeJukebox.Infrastructure.Music.Manual;
using OfficeJukebox.Infrastructure.Music.Spotify;
using OfficeJukebox.Infrastructure.Music.YouTube;
using OfficeJukebox.Infrastructure.Persistence;

namespace OfficeJukebox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddOptions<MusicProvidersOptions>().BindConfiguration(MusicProvidersOptions.SectionName);
        services.AddDataProtection();
        services.AddHttpClient("spotify");
        services.AddHttpClient("spotify-auth");
        services.AddHttpClient("youtube");

        services.AddScoped<IProviderTokenService, ProviderTokenService>();
        services.AddScoped<IProviderAuthService, SpotifyAuthService>();
        services.AddScoped<ManualCatalogProvider>();
        services.AddScoped<SpotifyCatalogProvider>();
        services.AddSingleton<AppleMusicCatalogProvider>();
        services.AddScoped<YouTubeCatalogProvider>();

        services.AddScoped<IMusicCatalogProvider, ManualCatalogProvider>();
        services.AddScoped<IMusicCatalogProvider>(sp => ResolveProvider(
            sp,
            "spotify",
            () => sp.GetRequiredService<SpotifyCatalogProvider>(),
            options => options.Spotify.Enabled));
        services.AddScoped<IMusicCatalogProvider>(sp => ResolveProvider(
            sp,
            "apple-music",
            () => sp.GetRequiredService<AppleMusicCatalogProvider>(),
            options => options.AppleMusic.Enabled));
        services.AddScoped<IMusicCatalogProvider>(sp => ResolveProvider(
            sp,
            "youtube",
            () => sp.GetRequiredService<YouTubeCatalogProvider>(),
            options => options.YouTube.Enabled));

        services.AddScoped<IMusicProviderRegistry, MusicProviderRegistry>();
        return services;
    }

    private static IMusicCatalogProvider ResolveProvider(
        IServiceProvider serviceProvider,
        string providerId,
        Func<IMusicCatalogProvider> enabledFactory,
        Func<MusicProvidersOptions, bool> isEnabled)
    {
        var options = serviceProvider.GetRequiredService<IOptions<MusicProvidersOptions>>().Value;
        return isEnabled(options) ? enabledFactory() : new DisabledCatalogProvider(providerId);
    }
}

internal sealed class DisabledCatalogProvider(string providerId) : IMusicCatalogProvider
{
    public string ProviderId => providerId;
    public ProviderCapabilities Capabilities => ProviderCapabilities.None;

    public Task<IReadOnlyList<Domain.Entities.Track>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Domain.Entities.Track>>([]);

    public Task<Domain.Entities.Track> ResolveAsync(Domain.ValueObjects.TrackRef trackRef, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException($"Provider {providerId} is disabled.");
}
