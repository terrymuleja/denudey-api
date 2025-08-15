using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.StatsDb
{
    /// <inheritdoc />
    public partial class AddValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualReview",
                table: "UserRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedAt",
                table: "UserRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ValidationConfidence",
                table: "UserRequests",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ValidationStatus",
                table: "UserRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresManualReview",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "ValidatedAt",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "ValidationConfidence",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "UserRequests");
        }
    }
}
