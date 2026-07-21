namespace OfficeJukebox.Api.Services;

public interface IPlayerClient
{
    Task<HttpResponseMessage> GetQueueAsync(CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> QueueTrackAsync(object request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetNowPlayingAsync(CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> VetoAsync(Guid id, object request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> SkipAsync(Guid id, object request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetDevicesAsync(string provider, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> SetDeviceAsync(object request, CancellationToken cancellationToken = default);
}

public sealed class PlayerClient(HttpClient httpClient) : IPlayerClient
{
    public Task<HttpResponseMessage> GetQueueAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetAsync("/queue", cancellationToken);

    public Task<HttpResponseMessage> QueueTrackAsync(object request, CancellationToken cancellationToken = default) =>
        httpClient.PostAsJsonAsync("/queue", request, cancellationToken);

    public Task<HttpResponseMessage> GetNowPlayingAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetAsync("/now-playing", cancellationToken);

    public Task<HttpResponseMessage> VetoAsync(Guid id, object request, CancellationToken cancellationToken = default) =>
        httpClient.PostAsJsonAsync($"/queue/{id}/veto", request, cancellationToken);

    public Task<HttpResponseMessage> SkipAsync(Guid id, object request, CancellationToken cancellationToken = default) =>
        httpClient.PostAsJsonAsync($"/queue/{id}/skip", request, cancellationToken);

    public Task<HttpResponseMessage> GetDevicesAsync(string provider, CancellationToken cancellationToken = default) =>
        httpClient.GetAsync($"/playback/devices?provider={Uri.EscapeDataString(provider)}", cancellationToken);

    public Task<HttpResponseMessage> SetDeviceAsync(object request, CancellationToken cancellationToken = default) =>
        httpClient.PutAsJsonAsync("/playback/device", request, cancellationToken);
}
