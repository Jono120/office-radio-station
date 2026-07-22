using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Domain.Repositories;

public interface ITrackPlayRepository
{
    IQueryable<TrackPlay> GetAll();
    Task<TrackPlay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TrackPlay trackPlay, CancellationToken cancellationToken = default);
    Task AddVetoAsync(TrackPlayVeto veto, CancellationToken cancellationToken = default);
    Task UpdateAsync(TrackPlay trackPlay, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrackPlay>> GetByStatusAsync(TrackPlayStatus status, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
