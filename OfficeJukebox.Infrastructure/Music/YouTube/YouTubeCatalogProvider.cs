using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Infrastructure.Music.YouTube;

public sealed class YouTubeCatalogProvider : IMusicCatalogProvider
{
    public string ProviderId => "youtube";
    public ProviderCapabilities Capabilities =>
        ProviderCapabilities.Search | ProviderCapabilities.Resolve | ProviderCapabilities.RequiresAuth;

    public Task<IReadOnlyList<Track>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Track>>([]);

    public Task<Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default) =>
        Task.FromResult(new Track
        {
            Name = trackRef.ExternalId,
            Link = $"https://www.youtube.com/watch?v={trackRef.ExternalId}"
        });
}
