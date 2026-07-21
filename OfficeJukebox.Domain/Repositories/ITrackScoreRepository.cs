using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Domain.Repositories;

public interface ITrackScoreRepository
{
    IQueryable<TrackScore> GetAll();
    Task ReplaceAllAsync(IReadOnlyList<TrackScore> scores, CancellationToken cancellationToken = default);
}
