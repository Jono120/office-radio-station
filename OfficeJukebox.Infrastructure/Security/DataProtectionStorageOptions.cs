namespace OfficeJukebox.Infrastructure.Security;

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "Security:DataProtection";

    public const string DefaultKeysPath = "%LOCALAPPDATA%/OfficeJukebox/keys";

    public string KeysPath { get; set; } = DefaultKeysPath;
}
