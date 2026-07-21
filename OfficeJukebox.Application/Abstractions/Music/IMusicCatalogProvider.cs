using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Abstractions.Music;

public interface IMusicCatalogProvider : IMusicProvider
{
    Task<IReadOnlyList<Track>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);
    Task<Track> ResolveAsync(TrackRef trackRef, CancellationToken cancellationToken = default);
}
