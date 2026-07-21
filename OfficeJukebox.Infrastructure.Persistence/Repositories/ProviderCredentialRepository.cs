using Microsoft.EntityFrameworkCore;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;

namespace OfficeJukebox.Infrastructure.Persistence.Repositories;

public sealed class ProviderCredentialRepository(JukeboxDbContext dbContext) : IProviderCredentialRepository
{
    public Task<ProviderCredential?> GetByProviderAsync(string provider, CancellationToken cancellationToken = default) =>
        dbContext.ProviderCredentials.FirstOrDefaultAsync(c => c.Provider == provider, cancellationToken);

    public async Task UpsertAsync(ProviderCredential credential, CancellationToken cancellationToken = default)
    {
        var existing = await GetByProviderAsync(credential.Provider, cancellationToken);
        if (existing is null)
        {
            await dbContext.ProviderCredentials.AddAsync(credential, cancellationToken);
        }
        else
        {
            existing.EncryptedAccessToken = credential.EncryptedAccessToken;
            existing.EncryptedRefreshToken = credential.EncryptedRefreshToken;
            existing.ExpiresAt = credential.ExpiresAt;
            existing.Scopes = credential.Scopes;
            existing.UpdatedAt = credential.UpdatedAt;
            dbContext.ProviderCredentials.Update(existing);
        }
    }

    public async Task DeleteByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var existing = await GetByProviderAsync(provider, cancellationToken);
        if (existing is not null)
        {
            dbContext.ProviderCredentials.Remove(existing);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
