using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ViewersAndFans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeLikes_Users_UserId",
                table: "EpisodeLikes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeViews_Users_UserId",
                table: "EpisodeViews",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeLikes_Users_UserId",
                table: "EpisodeLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeViews_Users_UserId",
                table: "EpisodeViews");
        }
    }
}
