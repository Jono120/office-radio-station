using OfficeJukebox.Domain.ValueObjects;

namespace OfficeJukebox.Application.Abstractions.Music;

public interface IMusicProvider
{
    string ProviderId { get; }
    ProviderCapabilities Capabilities { get; }
}
