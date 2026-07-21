namespace OfficeJukebox.Domain.ValueObjects;

public readonly record struct TrackRef(string Provider, string ExternalId)
{
    public static TrackRef Manual(string externalId) => new("manual", externalId);

    public bool IsEmpty => string.IsNullOrWhiteSpace(Provider) || string.IsNullOrWhiteSpace(ExternalId);

    public override string ToString() => $"{Provider}:{ExternalId}";
}
