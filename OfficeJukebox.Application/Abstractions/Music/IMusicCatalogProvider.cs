using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Abstractions.Music;

public interface IMusicCatalogProvider : IMusicProvider
{
    Task<IReadOnlyList<CatalogSearchResult>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the provider can serve requests right now (tokens present,
    /// API key configured, etc.). Each provider owns its own readiness rules
    /// instead of the controller special-casing provider ids.
    /// </summary>
    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
}
