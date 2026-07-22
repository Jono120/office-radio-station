namespace OfficeJukebox.Application.Configuration;

public sealed class OrganizationOptions
{
    public const string SectionName = "Organization";

    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Time zone id for the office location, set by the admin (Windows or IANA id,
    /// e.g. "New Zealand Standard Time" or "Pacific/Auckland").
    /// Empty means: use the machine's local time zone.
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;
}
