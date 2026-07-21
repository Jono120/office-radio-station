using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<IProviderTokenService, ProviderTokenService>();
        services.AddScoped<IProviderAuthService, SpotifyAuthService>();
        services.AddSingleton<ManualCatalogProvider>();
        services.AddScoped<SpotifyCatalogProvider>();
        services.AddSingleton<AppleMusicCatalogProvider>();
        services.AddSingleton<YouTubeCatalogProvider>();

        services.AddSingleton<IMusicCatalogProvider>(sp => sp.GetRequiredService<ManualCatalogProvider>());
        services.AddSingleton<IMusicCatalogProvider>(sp =>
        {
            var options = configuration.GetSection(MusicProvidersOptions.SectionName).Get<MusicProvidersOptions>() ?? new();
            return options.Spotify.Enabled ? sp.GetRequiredService<SpotifyCatalogProvider>() : new DisabledCatalogProvider("spotify");
        });
        services.AddSingleton<IMusicCatalogProvider>(sp =>
        {
            var options = configuration.GetSection(MusicProvidersOptions.SectionName).Get<MusicProvidersOptions>() ?? new();
            return options.AppleMusic.Enabled ? sp.GetRequiredService<AppleMusicCatalogProvider>() : new DisabledCatalogProvider("apple-music");
        });
        services.AddSingleton<IMusicCatalogProvider>(sp =>
        {
            var options = configuration.GetSection(MusicProvidersOptions.SectionName).Get<MusicProvidersOptions>() ?? new();
            return options.YouTube.Enabled ? sp.GetRequiredService<YouTubeCatalogProvider>() : new DisabledCatalogProvider("youtube");
        });

        services.AddSingleton<IMusicProviderRegistry, MusicProviderRegistry>();
        return services;
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
