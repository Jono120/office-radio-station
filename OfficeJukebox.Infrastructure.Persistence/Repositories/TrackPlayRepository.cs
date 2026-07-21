using Microsoft.EntityFrameworkCore;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;

namespace OfficeJukebox.Infrastructure.Persistence.Repositories;

public sealed class TrackPlayRepository(JukeboxDbContext dbContext) : ITrackPlayRepository
{
    public IQueryable<TrackPlay> GetAll() =>
        dbContext.TrackPlays
            .Include(t => t.Vetoes)
            .Include(t => t.Likes)
            .AsNoTracking();

    public Task<TrackPlay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TrackPlays
            .Include(t => t.Vetoes)
            .Include(t => t.Likes)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task AddAsync(TrackPlay trackPlay, CancellationToken cancellationToken = default)
    {
        await dbContext.TrackPlays.AddAsync(trackPlay, cancellationToken);
    }

    public Task UpdateAsync(TrackPlay trackPlay, CancellationToken cancellationToken = default)
    {
        dbContext.TrackPlays.Update(trackPlay);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TrackPlay>> GetQueuedAsync(CancellationToken cancellationToken = default) =>
        await dbContext.TrackPlays
            .Where(t => t.Status == TrackPlayStatus.Queued)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
