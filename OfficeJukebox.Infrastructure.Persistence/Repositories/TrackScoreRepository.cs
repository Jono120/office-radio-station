using Microsoft.EntityFrameworkCore;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;

namespace OfficeJukebox.Infrastructure.Persistence.Repositories;

public sealed class TrackScoreRepository(JukeboxDbContext dbContext) : ITrackScoreRepository
{
    public IQueryable<TrackScore> GetAll() => dbContext.TrackScores.AsNoTracking();

    public async Task ReplaceAllAsync(IReadOnlyList<TrackScore> scores, CancellationToken cancellationToken = default)
    {
        await dbContext.TrackScores.ExecuteDeleteAsync(cancellationToken);
        await dbContext.TrackScores.AddRangeAsync(scores, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
