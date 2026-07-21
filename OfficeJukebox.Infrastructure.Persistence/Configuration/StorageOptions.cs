namespace OfficeJukebox.Infrastructure.Persistence.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "Sqlite";
    public string ConnectionString { get; set; } = "Data Source=jukebox.db";
}
