using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Infrastructure.Music;
using OfficeJukebox.Infrastructure.Music.Auth;
using OfficeJukebox.Infrastructure.Music.Manual;
using OfficeJukebox.Infrastructure.Music.Spotify;
using OfficeJukebox.Infrastructure.Music.YouTube;
using OfficeJukebox.Infrastructure.Persistence;
using OfficeJukebox.Infrastructure.Security;

namespace OfficeJukebox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddOptions<MusicProvidersOptions>().BindConfiguration(MusicProvidersOptions.SectionName);

        var dataProtectionOptions = configuration.GetSection(DataProtectionStorageOptions.SectionName)
            .Get<DataProtectionStorageOptions>() ?? new DataProtectionStorageOptions();
        var keysDirectory = DataProtectionPathResolver.EnsureWritableKeysDirectory(
            DataProtectionPathResolver.ResolveKeysPath(dataProtectionOptions.KeysPath));

        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("OfficeJukebox")
            .PersistKeysToFileSystem(keysDirectory);

        // ProtectKeysWithDpapi is Windows-only; guard to avoid CA1416 on net10.0.
        if (OperatingSystem.IsWindows())
        {
            dataProtectionBuilder.ProtectKeysWithDpapi();
        }
        services.AddHttpClient("spotify");
        services.AddHttpClient("spotify-auth");
        services.AddHttpClient("youtube");

        services.AddScoped<IProviderTokenService, ProviderTokenService>();
        services.AddScoped<IProviderAuthService, SpotifyAuthService>();

        // Only enabled providers are registered at all: disabled ones no longer
        // appear in the registry, so ListEnabled() is truthful by construction.
        var musicOptions = configuration.GetSection(MusicProvidersOptions.SectionName)
            .Get<MusicProvidersOptions>() ?? new MusicProvidersOptions();

        if (musicOptions.Manual.Enabled)
        {
            services.AddScoped<IMusicCatalogProvider, ManualCatalogProvider>();
        }

        if (musicOptions.Spotify.Enabled)
        {
            services.AddScoped<IMusicCatalogProvider, SpotifyCatalogProvider>();
        }

        if (musicOptions.YouTube.Enabled)
        {
            services.AddScoped<IMusicCatalogProvider, YouTubeCatalogProvider>();
        }

        services.AddScoped<IMusicProviderRegistry, MusicProviderRegistry>();
        return services;
    }
}
