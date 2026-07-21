using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeJukebox.Infrastructure.Persistence.Migrations
{
    public partial class MultiProviderModernization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "TrackPlays",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TrackPlays",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Queued");

            migrationBuilder.AddColumn<string>(
                name: "Track_ArtistsJson",
                table: "TrackPlays",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<long>(
                name: "Track_DurationMilliseconds",
                table: "TrackPlays",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Track_TrackArtworkUrl",
                table: "TrackPlays",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "TrackScores",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "TrackScores",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProviderCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Scopes = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackPlays_Status_Id",
                table: "TrackPlays",
                columns: new[] { "Status", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackScores_Provider_ExternalId",
                table: "TrackScores",
                columns: new[] { "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCredentials_Provider",
                table: "ProviderCredentials",
                column: "Provider",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProviderCredentials");
            migrationBuilder.DropIndex(name: "IX_TrackScores_Provider_ExternalId", table: "TrackScores");
            migrationBuilder.DropIndex(name: "IX_TrackPlays_Status_Id", table: "TrackPlays");
            migrationBuilder.DropColumn(name: "ExternalId", table: "TrackPlays");
            migrationBuilder.DropColumn(name: "Status", table: "TrackPlays");
            migrationBuilder.DropColumn(name: "Track_ArtistsJson", table: "TrackPlays");
            migrationBuilder.DropColumn(name: "Track_DurationMilliseconds", table: "TrackPlays");
            migrationBuilder.DropColumn(name: "Track_TrackArtworkUrl", table: "TrackPlays");
            migrationBuilder.DropColumn(name: "Provider", table: "TrackScores");
            migrationBuilder.DropColumn(name: "ExternalId", table: "TrackScores");
        }
    }
}
