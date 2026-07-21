using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeJukebox.Domain.Entities;
using OfficeJukebox.Domain.Repositories;

namespace OfficeJukebox.Infrastructure.Music.Auth;

public interface IProviderTokenService
{
    Task<string?> GetAccessTokenAsync(string provider, CancellationToken cancellationToken = default);
    Task StoreTokensAsync(string provider, string accessToken, string? refreshToken, DateTime? expiresAt, string? scopes, CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(string provider, CancellationToken cancellationToken = default);
    Task DisconnectAsync(string provider, CancellationToken cancellationToken = default);
}

public interface IProviderAuthService
{
    string ProviderId { get; }
    string BuildAuthorizationUrl(string state);
    Task<TokenRefreshResult> CompleteAuthorizationAsync(string code, CancellationToken cancellationToken = default);
    Task<TokenRefreshResult?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public sealed record TokenRefreshResult(string AccessToken, string? RefreshToken, int ExpiresIn, string? Scope);

public sealed class ProviderTokenService(
    IProviderCredentialRepository credentialRepository,
    IEnumerable<IProviderAuthService> authServices,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<ProviderTokenService> logger) : IProviderTokenService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("OfficeJukebox.ProviderTokens");
    private readonly IReadOnlyDictionary<string, IProviderAuthService> _authByProvider =
        authServices.ToDictionary(a => a.ProviderId, StringComparer.OrdinalIgnoreCase);

    public async Task<string?> GetAccessTokenAsync(string provider, CancellationToken cancellationToken = default)
    {
        var credential = await credentialRepository.GetByProviderAsync(provider, cancellationToken);
        if (credential is null)
        {
            return null;
        }

        if (!IsExpired(credential.ExpiresAt))
        {
            return _protector.Unprotect(credential.EncryptedAccessToken);
        }

        if (string.IsNullOrWhiteSpace(credential.EncryptedRefreshToken))
        {
            return null;
        }

        if (!_authByProvider.TryGetValue(provider, out var authService))
        {
            return null;
        }

        var refreshToken = _protector.Unprotect(credential.EncryptedRefreshToken);
        var refreshed = await authService.RefreshAccessTokenAsync(refreshToken, cancellationToken);
        if (refreshed is null)
        {
            logger.LogWarning("Failed to refresh access token for provider {Provider}", provider);
            return null;
        }

        var expiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn);
        await StoreTokensAsync(provider, refreshed.AccessToken, refreshed.RefreshToken ?? refreshToken, expiresAt, refreshed.Scope, cancellationToken);
        return refreshed.AccessToken;
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

    public async Task DisconnectAsync(string provider, CancellationToken cancellationToken = default)
    {
        await credentialRepository.DeleteByProviderAsync(provider, cancellationToken);
        await credentialRepository.SaveChangesAsync(cancellationToken);
    }

    private static bool IsExpired(DateTime? expiresAt) =>
        expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow.AddMinutes(1);
}

public sealed class SpotifyAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<MusicProvidersOptions> options) : IProviderAuthService
{
    private const string Scopes =
        "user-read-playback-state user-modify-playback-state user-read-currently-playing";

    public string ProviderId => "spotify";

    public string BuildAuthorizationUrl(string state)
    {
        var spotify = options.Value.Spotify;
        if (string.IsNullOrWhiteSpace(spotify.ClientId) || string.IsNullOrWhiteSpace(spotify.RedirectUri))
        {
            throw new InvalidOperationException("Spotify ClientId and RedirectUri must be configured.");
        }

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = spotify.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = spotify.RedirectUri,
            ["scope"] = Scopes,
            ["state"] = state,
            ["show_dialog"] = "true"
        };
        return QueryHelpers.Build("https://accounts.spotify.com/authorize", query);
    }

    public async Task<TokenRefreshResult> CompleteAuthorizationAsync(string code, CancellationToken cancellationToken = default)
    {
        var tokens = await ExchangeTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = options.Value.Spotify.RedirectUri ?? string.Empty
        }, cancellationToken);

        return new TokenRefreshResult(tokens.access_token, tokens.refresh_token, tokens.expires_in, tokens.scope);
    }

    public async Task<TokenRefreshResult?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await ExchangeTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            }, cancellationToken);

            return new TokenRefreshResult(tokens.access_token, tokens.refresh_token, tokens.expires_in, tokens.scope);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task<TokenResponse> ExchangeTokenAsync(
        Dictionary<string, string> body,
        CancellationToken cancellationToken)
    {
        var spotify = options.Value.Spotify;
        if (string.IsNullOrWhiteSpace(spotify.ClientId) || string.IsNullOrWhiteSpace(spotify.ClientSecret))
        {
            throw new InvalidOperationException("Spotify ClientId and ClientSecret must be configured.");
        }

        body["client_id"] = spotify.ClientId;
        body["client_secret"] = spotify.ClientSecret;

        var client = httpClientFactory.CreateClient("spotify-auth");
        var response = await client.PostAsync(
            "https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(body),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Invalid token response from Spotify.");
    }

    private sealed record TokenResponse(string access_token, string? refresh_token, int expires_in, string? scope);
}

internal static class QueryHelpers
{
    public static string Build(string baseUrl, IReadOnlyDictionary<string, string?> query)
    {
        var pairs = query
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}");
        return $"{baseUrl}?{string.Join("&", pairs)}";
    }
}
