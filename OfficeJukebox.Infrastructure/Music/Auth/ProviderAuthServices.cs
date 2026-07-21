using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;

namespace OfficeJukebox.Infrastructure.Music.Auth;

public interface IProviderTokenService
{
    Task<string?> GetAccessTokenAsync(string provider, CancellationToken cancellationToken = default);
    Task StoreTokensAsync(string provider, string accessToken, string? refreshToken, DateTime? expiresAt, string? scopes, CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(string provider, CancellationToken cancellationToken = default);
}

public interface IProviderAuthService
{
    string ProviderId { get; }
    string BuildAuthorizationUrl(string state);
    Task CompleteAuthorizationAsync(string code, CancellationToken cancellationToken = default);
}

public sealed class ProviderTokenService(
    IProviderCredentialRepository credentialRepository,
    IDataProtectionProvider dataProtectionProvider) : IProviderTokenService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("OfficeJukebox.ProviderTokens");

    public async Task<string?> GetAccessTokenAsync(string provider, CancellationToken cancellationToken = default)
    {
        var credential = await credentialRepository.GetByProviderAsync(provider, cancellationToken);
        if (credential is null)
        {
            return null;
        }

        if (credential.ExpiresAt.HasValue && credential.ExpiresAt.Value <= DateTime.UtcNow.AddMinutes(1))
        {
            return null;
        }

        return _protector.Unprotect(credential.EncryptedAccessToken);
    }

    public async Task StoreTokensAsync(
        string provider,
        string accessToken,
        string? refreshToken,
        DateTime? expiresAt,
        string? scopes,
        CancellationToken cancellationToken = default)
    {
        var credential = new ProviderCredential
        {
            Provider = provider,
            EncryptedAccessToken = _protector.Protect(accessToken),
            EncryptedRefreshToken = refreshToken is null ? null : _protector.Protect(refreshToken),
            ExpiresAt = expiresAt,
            Scopes = scopes,
            UpdatedAt = DateTime.UtcNow
        };
        await credentialRepository.UpsertAsync(credential, cancellationToken);
        await credentialRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsAuthenticatedAsync(string provider, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(provider, cancellationToken);
        return !string.IsNullOrWhiteSpace(token);
    }
}

public sealed class SpotifyAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<MusicProvidersOptions> options,
    IProviderTokenService tokenService) : IProviderAuthService
{
    public string ProviderId => "spotify";

    public string BuildAuthorizationUrl(string state)
    {
        var spotify = options.Value.Spotify;
        var scopes = Uri.EscapeDataString("user-read-playback-state user-modify-playback-state user-read-currently-playing");
        return $"https://accounts.spotify.com/authorize?client_id={Uri.EscapeDataString(spotify.ClientId ?? string.Empty)}&response_type=code&redirect_uri={Uri.EscapeDataString(spotify.RedirectUri ?? string.Empty)}&scope={scopes}&state={Uri.EscapeDataString(state)}";
    }

    public async Task CompleteAuthorizationAsync(string code, CancellationToken cancellationToken = default)
    {
        var spotify = options.Value.Spotify;
        var client = httpClientFactory.CreateClient();
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = spotify.RedirectUri ?? string.Empty,
            ["client_id"] = spotify.ClientId ?? string.Empty,
            ["client_secret"] = spotify.ClientSecret ?? string.Empty
        };
        var response = await client.PostAsync("https://accounts.spotify.com/api/token", new FormUrlEncodedContent(body), cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Invalid token response.");
        var expiresAt = DateTime.UtcNow.AddSeconds(json.expires_in);
        await tokenService.StoreTokensAsync(ProviderId, json.access_token, json.refresh_token, expiresAt, json.scope, cancellationToken);
    }

    private sealed record TokenResponse(string access_token, string? refresh_token, int expires_in, string? scope);
}
