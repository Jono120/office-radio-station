namespace OfficeJukebox.Infrastructure.Security;

internal static class DataProtectionPathResolver
{
    public static string ResolveKeysPath(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = DataProtectionStorageOptions.DefaultKeysPath;
        }

        if (!configured.Contains("%LOCALAPPDATA%", StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return configured.Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase);
    }

    public static DirectoryInfo EnsureWritableKeysDirectory(string keysPath)
    {
        var directory = new DirectoryInfo(keysPath);
        try
        {
            directory.Create();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot create the Data Protection keys directory at '{keysPath}'.", ex);
        }

        var probe = Path.Combine(directory.FullName, $".write-probe-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"The Data Protection keys directory at '{directory.FullName}' is not writable.", ex);
        }

        return directory;
    }
}
