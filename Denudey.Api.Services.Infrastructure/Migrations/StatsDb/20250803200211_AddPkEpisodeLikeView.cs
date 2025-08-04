using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.StatsDb
{
    /// <inheritdoc />
    public partial class AddPkEpisodeLikeView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodeLikes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeId = table.Column<int>(type: "integer", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorUsername = table.Column<string>(type: "text", nullable: false),
                    CreatorProfileImageUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeLikes", x => new { x.UserId, x.EpisodeId });
                });

            migrationBuilder.CreateTable(
                name: "EpisodeViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeId = table.Column<int>(type: "integer", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorUsername = table.Column<string>(type: "text", nullable: false),
                    CreatorProfileImageUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeViews", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodeLikes");

            migrationBuilder.DropTable(
                name: "EpisodeViews");
        }
    }
}
