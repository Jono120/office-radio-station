using OfficeJukebox.Application.Abstractions.Music;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Infrastructure.Music.Manual;

public sealed class ManualCatalogProvider : IMusicCatalogProvider
{
    public string ProviderId => "manual";
    public ProviderCapabilities Capabilities => ProviderCapabilities.Resolve;

    public Task<IReadOnlyList<Track>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Track>>([]);

    public Task<Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default)
    {
        var track = new Track
        {
            Name = trackRef.ExternalId,
            Link = string.Empty
        };
        return Task.FromResult(track);
    }
}
