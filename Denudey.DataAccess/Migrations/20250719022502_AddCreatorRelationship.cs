using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.Sql("""
                                     ALTER TABLE "ScamflixEpisodes"
                                     ALTER COLUMN "CreatedBy" TYPE uuid USING "CreatedBy"::uuid;
                                 """);


            migrationBuilder.CreateIndex(
                name: "IX_ScamflixEpisodes_CreatedBy",
                table: "ScamflixEpisodes",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ScamflixEpisodes_Users_CreatedBy",
                table: "ScamflixEpisodes",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScamflixEpisodes_Users_CreatedBy",
                table: "ScamflixEpisodes");

            migrationBuilder.DropIndex(
                name: "IX_ScamflixEpisodes_CreatedBy",
                table: "ScamflixEpisodes");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "ScamflixEpisodes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 100);
        }
    }
}
