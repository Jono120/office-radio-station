namespace OfficeJukebox.Infrastructure.Music;

public sealed class MusicProvidersOptions
{
    public const string SectionName = "MusicProviders";

    public string? WebAppUrl { get; set; }
    public ProviderOptions Spotify { get; set; } = new();
    public ProviderOptions YouTube { get; set; } = new();
    public ProviderOptions Manual { get; set; } = new() { Enabled = true };
}

public sealed class ProviderOptions
{
    public bool Enabled { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public string? PreferredDeviceId { get; set; }
    public string? DeveloperToken { get; set; }
    public string? ApiKey { get; set; }
}
