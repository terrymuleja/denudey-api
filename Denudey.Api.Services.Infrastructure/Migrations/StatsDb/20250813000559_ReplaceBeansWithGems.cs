using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.StatsDb
{
    /// <inheritdoc />
    public partial class ReplaceBeansWithGems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BeanBalance",
                table: "UserWallets",
                newName: "GemBalance");

            migrationBuilder.RenameIndex(
                name: "IX_UserWallets_BeanBalance",
                table: "UserWallets",
                newName: "IX_UserWallets_GemBalance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GemBalance",
                table: "UserWallets",
                newName: "BeanBalance");

            migrationBuilder.RenameIndex(
                name: "IX_UserWallets_GemBalance",
                table: "UserWallets",
                newName: "IX_UserWallets_BeanBalance");
        }
    }
}
