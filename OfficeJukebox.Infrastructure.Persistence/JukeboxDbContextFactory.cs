using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OfficeJukebox.Infrastructure.Persistence;

public sealed class JukeboxDbContextFactory : IDesignTimeDbContextFactory<JukeboxDbContext>
{
    public JukeboxDbContext CreateDbContext(string[] args)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(localAppData, "OfficeJukebox", "jukebox.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var optionsBuilder = new DbContextOptionsBuilder<JukeboxDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new JukeboxDbContext(optionsBuilder.Options);
    }
}
