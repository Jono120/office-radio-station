namespace OfficeJukebox.Application.Abstractions.Music;

public interface IMusicProviderRegistry
{
    IMusicCatalogProvider? GetCatalog(string providerId);
    IMusicPlaybackController? GetPlayback(string providerId);
    IReadOnlyList<ProviderInfo> ListEnabled();
    IReadOnlyList<IMusicCatalogProvider> GetAllCatalogProviders();
}
