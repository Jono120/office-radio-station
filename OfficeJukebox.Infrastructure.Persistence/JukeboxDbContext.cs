using Microsoft.EntityFrameworkCore;
using OfficeJukebox.Domain.Entities;

namespace OfficeJukebox.Infrastructure.Persistence;

public sealed class JukeboxDbContext(DbContextOptions<JukeboxDbContext> options) : DbContext(options)
{
    public DbSet<TrackPlay> TrackPlays => Set<TrackPlay>();
    public DbSet<TrackPlayVeto> TrackPlayVetoes => Set<TrackPlayVeto>();
    public DbSet<TrackPlayLike> TrackPlayLikes => Set<TrackPlayLike>();
    public DbSet<TrackScore> TrackScores => Set<TrackScore>();
    public DbSet<SearchTerm> SearchTerms => Set<SearchTerm>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<RickRollTarget> RickRollTargets => Set<RickRollTarget>();
    public DbSet<SoundBoardEvent> SoundBoardEvents => Set<SoundBoardEvent>();
    public DbSet<ProviderCredential> ProviderCredentials => Set<ProviderCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackPlay>(entity =>
        {
            entity.ToTable("TrackPlays");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.User).IsRequired().HasMaxLength(256);
            entity.Property(e => e.TrackJson).IsRequired();
            entity.Property(e => e.Provider).HasMaxLength(64);
            entity.Property(e => e.ExternalId).HasMaxLength(256);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.OwnsOne(e => e.Track, track =>
            {
                track.Property(t => t.Name).HasMaxLength(512);
                track.Property(t => t.Link).HasMaxLength(1024);
                track.Property(t => t.DurationMilliseconds);
                track.Property(t => t.TrackArtworkUrl).HasMaxLength(1024);
                track.Property(t => t.ArtistsJson).HasMaxLength(2048);
                track.Ignore(t => t.Artists);
                track.OwnsOne(t => t.Album, album => album.Property(a => a.Name).HasMaxLength(512));
            });
            entity.HasMany(e => e.Vetoes).WithOne(v => v.TrackPlay).HasForeignKey(v => v.TrackPlayId);
            entity.HasMany(e => e.Likes).WithOne(l => l.TrackPlay).HasForeignKey(l => l.TrackPlayId);
            entity.HasIndex(e => new { e.Status, e.Id });
        });

        modelBuilder.Entity<TrackPlayVeto>(entity =>
        {
            entity.ToTable("TrackPlayVetoes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ByUser).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<TrackPlayLike>(entity =>
        {
            entity.ToTable("TrackPlayLikes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ByUser).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<TrackScore>(entity =>
        {
            entity.ToTable("TrackScores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ExternalLink).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.TrackJson).IsRequired();
            entity.HasIndex(e => new { e.Provider, e.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<ProviderCredential>(entity =>
        {
            entity.ToTable("ProviderCredentials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(64);
            entity.Property(e => e.EncryptedAccessToken).IsRequired();
            entity.HasIndex(e => e.Provider).IsUnique();
        });

        modelBuilder.Entity<SearchTerm>(entity =>
        {
            entity.ToTable("SearchTerms");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Term).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("AdminUsers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(128);
        });

        modelBuilder.Entity<RickRollTarget>(entity =>
        {
            entity.ToTable("RickRollTargets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(128);
        });

        modelBuilder.Entity<SoundBoardEvent>(entity =>
        {
            entity.ToTable("SoundBoardEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
        });
    }
}
