using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.StatsDb
{
    /// <inheritdoc />
    public partial class AddSocial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorProfileImageUrl",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "CreatorUsername",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "CreatorProfileImageUrl",
                table: "ProductLikes");

            migrationBuilder.DropColumn(
                name: "CreatorUsername",
                table: "ProductLikes");

            migrationBuilder.DropColumn(
                name: "CreatorProfileImageUrl",
                table: "EpisodeViews");

            migrationBuilder.DropColumn(
                name: "CreatorUsername",
                table: "EpisodeViews");

            migrationBuilder.DropColumn(
                name: "CreatorProfileImageUrl",
                table: "EpisodeLikes");

            migrationBuilder.DropColumn(
                name: "CreatorUsername",
                table: "EpisodeLikes");

            migrationBuilder.RenameColumn(
                name: "CreatorId",
                table: "EpisodeViews",
                newName: "RequesterId");

            migrationBuilder.RenameColumn(
                name: "CreatorId",
                table: "EpisodeLikes",
                newName: "RequesterId");

            migrationBuilder.CreateTable(
                name: "CreatorSocials",
                columns: table => new
                {
                    CreatorId = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "text", nullable: true),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorSocials", x => x.CreatorId);
                });

            migrationBuilder.CreateTable(
                name: "RequesterSocials",
                columns: table => new
                {
                    RequesterId = table.Column<Guid>(type: "uuid", maxLength: 255, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequesterSocials", x => x.RequesterId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductViews_CreatorId",
                table: "ProductViews",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViews_ProductId",
                table: "ProductViews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViews_UserId",
                table: "ProductViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLikes_CreatorId",
                table: "ProductLikes",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeViews_EpisodeId",
                table: "EpisodeViews",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeViews_RequesterId",
                table: "EpisodeViews",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeViews_UserId",
                table: "EpisodeViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeLikes_RequesterId",
                table: "EpisodeLikes",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorSocials_UpdatedAt",
                table: "CreatorSocials",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorSocials_Username",
                table: "CreatorSocials",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeLikes_RequesterSocials_RequesterId",
                table: "EpisodeLikes",
                column: "RequesterId",
                principalTable: "RequesterSocials",
                principalColumn: "RequesterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeViews_RequesterSocials_RequesterId",
                table: "EpisodeViews",
                column: "RequesterId",
                principalTable: "RequesterSocials",
                principalColumn: "RequesterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductLikes_CreatorSocials_CreatorId",
                table: "ProductLikes",
                column: "CreatorId",
                principalTable: "CreatorSocials",
                principalColumn: "CreatorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductViews_CreatorSocials_CreatorId",
                table: "ProductViews",
                column: "CreatorId",
                principalTable: "CreatorSocials",
                principalColumn: "CreatorId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeLikes_RequesterSocials_RequesterId",
                table: "EpisodeLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeViews_RequesterSocials_RequesterId",
                table: "EpisodeViews");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductLikes_CreatorSocials_CreatorId",
                table: "ProductLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductViews_CreatorSocials_CreatorId",
                table: "ProductViews");

            migrationBuilder.DropTable(
                name: "CreatorSocials");

            migrationBuilder.DropTable(
                name: "RequesterSocials");

            migrationBuilder.DropIndex(
                name: "IX_ProductViews_CreatorId",
                table: "ProductViews");

            migrationBuilder.DropIndex(
                name: "IX_ProductViews_ProductId",
                table: "ProductViews");

            migrationBuilder.DropIndex(
                name: "IX_ProductViews_UserId",
                table: "ProductViews");

            migrationBuilder.DropIndex(
                name: "IX_ProductLikes_CreatorId",
                table: "ProductLikes");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeViews_EpisodeId",
                table: "EpisodeViews");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeViews_RequesterId",
                table: "EpisodeViews");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeViews_UserId",
                table: "EpisodeViews");

            migrationBuilder.DropIndex(
                name: "IX_EpisodeLikes_RequesterId",
                table: "EpisodeLikes");

            migrationBuilder.RenameColumn(
                name: "RequesterId",
                table: "EpisodeViews",
                newName: "CreatorId");

            migrationBuilder.RenameColumn(
                name: "RequesterId",
                table: "EpisodeLikes",
                newName: "CreatorId");

            migrationBuilder.AddColumn<string>(
                name: "CreatorProfileImageUrl",
                table: "ProductViews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorUsername",
                table: "ProductViews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorProfileImageUrl",
                table: "ProductLikes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorUsername",
                table: "ProductLikes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorProfileImageUrl",
                table: "EpisodeViews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorUsername",
                table: "EpisodeViews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorProfileImageUrl",
                table: "EpisodeLikes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorUsername",
                table: "EpisodeLikes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
