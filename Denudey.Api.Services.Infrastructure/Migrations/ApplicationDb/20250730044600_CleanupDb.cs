using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class CleanupDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodeLikes");

            migrationBuilder.DropTable(
                name: "EpisodeViews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodeLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EpisodeId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LikedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeLikes_ScamflixEpisodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "ScamflixEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EpisodeId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeViews_ScamflixEpisodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "ScamflixEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeLikes_EpisodeId",
                table: "EpisodeLikes",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeLikes_UserId_EpisodeId",
                table: "EpisodeLikes",
                columns: new[] { "UserId", "EpisodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeViews_EpisodeId",
                table: "EpisodeViews",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeViews_UserId_EpisodeId_ViewedAt",
                table: "EpisodeViews",
                columns: new[] { "UserId", "EpisodeId", "ViewedAt" });
        }
    }
}
