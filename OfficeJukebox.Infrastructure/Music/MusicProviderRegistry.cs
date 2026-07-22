using OfficeJukebox.Application.Abstractions.Music;

namespace OfficeJukebox.Infrastructure.Music;

public sealed class MusicProviderRegistry(IEnumerable<IMusicCatalogProvider> catalogProviders) : IMusicProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IMusicCatalogProvider> _catalog =
        catalogProviders.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);

    public IMusicCatalogProvider? GetCatalog(string providerId) =>
        _catalog.TryGetValue(providerId, out var provider) ? provider : null;

    public IMusicPlaybackController? GetPlayback(string providerId) =>
        _catalog.TryGetValue(providerId, out var provider) && provider is IMusicPlaybackController playback
            ? playback
            : null;

    // Disabled providers are never registered (see Infrastructure DI), so
    // everything in the registry is enabled by construction.
    public IReadOnlyList<ProviderInfo> ListEnabled() =>
        _catalog.Values
            .Select(p => new ProviderInfo(p.ProviderId, GetDisplayName(p.ProviderId), p.Capabilities))
            .ToList();

    private static string GetDisplayName(string providerId) => providerId switch
    {
        "spotify" => "Spotify",
        "youtube" => "YouTube",
        "manual" => "Manual",
        _ => providerId
    };
}
