using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeJukebox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RickRollTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RickRollTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Term = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsForbidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchTerms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SoundBoardEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AudioUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoundBoardEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackPlays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    User = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Excluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSkipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    TrackJson = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExternalLink = table.Column<string>(type: "TEXT", nullable: true),
                    Track_Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Track_Link = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Track_Album_Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackPlays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalLink = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    IsExcluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    MillisecondsSinceLastPlay = table.Column<double>(type: "REAL", nullable: false),
                    TrackJson = table.Column<string>(type: "TEXT", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackPlayLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackPlayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ByUser = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LikedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackPlayLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackPlayLikes_TrackPlays_TrackPlayId",
                        column: x => x.TrackPlayId,
                        principalTable: "TrackPlays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackPlayVetoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackPlayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ByUser = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    VetoedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackPlayVetoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackPlayVetoes_TrackPlays_TrackPlayId",
                        column: x => x.TrackPlayId,
                        principalTable: "TrackPlays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackPlayLikes_TrackPlayId",
                table: "TrackPlayLikes",
                column: "TrackPlayId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackPlayVetoes_TrackPlayId",
                table: "TrackPlayVetoes",
                column: "TrackPlayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "RickRollTargets");

            migrationBuilder.DropTable(
                name: "SearchTerms");

            migrationBuilder.DropTable(
                name: "SoundBoardEvents");

            migrationBuilder.DropTable(
                name: "TrackPlayLikes");

            migrationBuilder.DropTable(
                name: "TrackPlayVetoes");

            migrationBuilder.DropTable(
                name: "TrackScores");

            migrationBuilder.DropTable(
                name: "TrackPlays");
        }
    }
}
