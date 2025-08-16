using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class CleanupDbXXXXXXXXXXXXXX : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductViews");
            migrationBuilder.DropTable(name: "ProductLikes");
            migrationBuilder.DropTable(name: "EpisodeViews");
            migrationBuilder.DropTable(name: "EpisodeLikes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
