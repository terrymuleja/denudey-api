using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.StatsDb
{
    /// <inheritdoc />
    public partial class AddProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "RequesterSocials",
                newName: "Username");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "RequesterSocials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "RequesterSocials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "RequesterSocials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "RequesterSocials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "CreatorSocials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "CreatorSocials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "CreatorSocials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "CreatorSocials",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "RequesterSocials");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "RequesterSocials");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "RequesterSocials");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "RequesterSocials");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "CreatorSocials");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "CreatorSocials");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "CreatorSocials");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "CreatorSocials");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "RequesterSocials",
                newName: "DisplayName");
        }
    }
}
