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

    // New vetoes must be inserted explicitly: the owning TrackPlay graph is a
    // detached instance held in memory across requests, and Update() would
    // mark a new child with a client-generated key as Modified (0-row UPDATE).
    public async Task AddVetoAsync(TrackPlayVeto veto, CancellationToken cancellationToken = default)
    {
        await dbContext.TrackPlayVetoes.AddAsync(veto, cancellationToken);
    }

    public Task UpdateAsync(TrackPlay trackPlay, CancellationToken cancellationToken = default)
    {
        dbContext.TrackPlays.Update(trackPlay);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TrackPlay>> GetByStatusAsync(TrackPlayStatus status, CancellationToken cancellationToken = default) =>
        await dbContext.TrackPlays
            .Where(t => t.Status == status)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
