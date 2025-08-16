using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.StatsDb
{
    /// <inheritdoc />
    public partial class AddValidationError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValidationError",
                table: "UserRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationError",
                table: "UserRequests");
        }
    }
}
