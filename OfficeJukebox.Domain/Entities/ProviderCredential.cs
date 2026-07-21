namespace OfficeJukebox.Domain.Entities;

public sealed class ProviderCredential : Entity
{
    public string Provider { get; set; } = string.Empty;
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string? EncryptedRefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Scopes { get; set; }
    public DateTime UpdatedAt { get; set; }
}
