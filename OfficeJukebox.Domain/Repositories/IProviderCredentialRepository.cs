using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Domain.Repositories;

public interface IProviderCredentialRepository
{
    Task<ProviderCredential?> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task UpsertAsync(ProviderCredential credential, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
