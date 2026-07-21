using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeJukebox.Domain.Repositories;
using OfficeJukebox.Infrastructure.Persistence.Configuration;
using OfficeJukebox.Infrastructure.Persistence.Repositories;

namespace OfficeJukebox.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var storage = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
                      ?? new StorageOptions();

        var connectionString = ResolveConnectionString(storage.ConnectionString);

        services.AddDbContext<JukeboxDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ITrackPlayRepository, TrackPlayRepository>();
        services.AddScoped<ITrackScoreRepository, TrackScoreRepository>();
        services.AddScoped<IProviderCredentialRepository, ProviderCredentialRepository>();

        return services;
    }

    private static string ResolveConnectionString(string configured)
    {
        if (!configured.Contains("%LOCALAPPDATA%", StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return configured.Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase);
    }
}
