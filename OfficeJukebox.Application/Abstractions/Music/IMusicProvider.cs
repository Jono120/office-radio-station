namespace OfficeJukebox.Application.Abstractions.Music;

public interface IMusicProvider
{
    string ProviderId { get; }
    ProviderCapabilities Capabilities { get; }

    /// <summary>
    /// Where an admin obtains credentials for this provider (developer
    /// dashboard / console). Null when the provider needs no setup.
    /// Keeping this on the provider means the controllers need no
    /// per-provider string checks.
    /// </summary>
    string? SetupUrl { get; }
}
