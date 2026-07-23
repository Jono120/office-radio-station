using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeJukebox.Infrastructure;

namespace OfficeJukebox.Api.Tests.Security;

/// <summary>
/// Proves Api and Player can share a key ring: ciphertext produced by one
/// process's IDataProtectionProvider is readable by another using the same path.
/// </summary>
public sealed class DataProtectionRoundTripTests : IDisposable
{
    private readonly string _keysPath =
        Path.Combine(Path.GetTempPath(), $"officejukebox-dp-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Protect_in_one_provider_unprotects_in_another_with_shared_keys()
    {
        var configuration = BuildConfiguration(_keysPath);

        using var apiServices = BuildInfrastructureProvider(configuration);
        using var playerServices = BuildInfrastructureProvider(configuration);

        var apiProtector = apiServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProviderTokenServicePurpose);
        var playerProtector = playerServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProviderTokenServicePurpose);

        const string plaintext = "spotify-access-token-value";
        var protectedPayload = apiProtector.Protect(plaintext);
        var roundTripped = playerProtector.Unprotect(protectedPayload);

        Assert.Equal(plaintext, roundTripped);
    }

    public void Dispose()
    {
        if (Directory.Exists(_keysPath))
        {
            Directory.Delete(_keysPath, recursive: true);
        }
    }

    private const string ProviderTokenServicePurpose = "OfficeJukebox.ProviderTokens";

    private static IConfiguration BuildConfiguration(string keysPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ConnectionString"] =
                    $"Data Source={Path.Combine(Path.GetTempPath(), $"officejukebox-dp-db-{Guid.NewGuid():N}.db")}",
                ["Security:DataProtection:KeysPath"] = keysPath,
                ["MusicProviders:Manual:Enabled"] = "true",
                ["MusicProviders:Spotify:Enabled"] = "false",
                ["MusicProviders:YouTube:Enabled"] = "false"
            })
            .Build();

    private static ServiceProvider BuildInfrastructureProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
