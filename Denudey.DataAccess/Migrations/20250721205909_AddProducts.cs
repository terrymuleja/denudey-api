using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    MainPhotoUrl = table.Column<string>(type: "text", nullable: false),
                    SecondaryPhotoUrls = table.Column<string>(type: "text", nullable: false),
                    BodyPart = table.Column<string>(type: "text", nullable: false),
                    DeliveryOptions = table.Column<string>(type: "text", nullable: false),
                    FeePerDelivery = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Demands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveryUrl = table.Column<string>(type: "text", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DeliveryDeadlineOption = table.Column<string>(type: "text", nullable: true),
                    DeadlineAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Demands_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demands_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Demands_ProductId",
                table: "Demands",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Demands_RequestedBy",
                table: "Demands",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedBy",
                table: "Products",
                column: "CreatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Demands");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
