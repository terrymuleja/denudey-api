using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Denudey.Api.Services.Infrastructure.Migrations.StatsDb
{
    /// <inheritdoc />
    public partial class AddRequestsWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BodyPart = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Text = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeliveredImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ExtraAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeadLine = table.Column<int>(type: "integer", nullable: false),
                    ExpectedDeliveredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BodyPartValidated = table.Column<bool>(type: "boolean", nullable: true),
                    TextValidated = table.Column<bool>(type: "boolean", nullable: true),
                    ManualValidated = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRequest_CreatorSocial",
                        column: x => x.CreatorId,
                        principalTable: "CreatorSocials",
                        principalColumn: "CreatorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRequest_RequesterSocial",
                        column: x => x.RequestorId,
                        principalTable: "RequesterSocials",
                        principalColumn: "RequesterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserWallets",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeanBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UsdBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallets", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_BodyPartValidated",
                table: "UserRequests",
                column: "BodyPartValidated");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_CreatedAt",
                table: "UserRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_CreatorId",
                table: "UserRequests",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_CreatorId_Status",
                table: "UserRequests",
                columns: new[] { "CreatorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_DeadLine",
                table: "UserRequests",
                column: "DeadLine");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_DeadLine_ExpectedDate",
                table: "UserRequests",
                columns: new[] { "DeadLine", "ExpectedDeliveredDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_DeliveredDate",
                table: "UserRequests",
                column: "DeliveredDate");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_ExpectedDeliveredDate",
                table: "UserRequests",
                column: "ExpectedDeliveredDate");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_ManualValidated",
                table: "UserRequests",
                column: "ManualValidated");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_ProductId",
                table: "UserRequests",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_RequestorId",
                table: "UserRequests",
                column: "RequestorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_RequestorId_Status",
                table: "UserRequests",
                columns: new[] { "RequestorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_Status",
                table: "UserRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_Status_CreatedAt",
                table: "UserRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_Status_DeliveredDate",
                table: "UserRequests",
                columns: new[] { "Status", "DeliveredDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_TextValidated",
                table: "UserRequests",
                column: "TextValidated");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_ValidationStatus",
                table: "UserRequests",
                columns: new[] { "BodyPartValidated", "TextValidated", "ManualValidated" });

            migrationBuilder.CreateIndex(
                name: "IX_UserWallets_BeanBalance",
                table: "UserWallets",
                column: "BeanBalance");

            migrationBuilder.CreateIndex(
                name: "IX_UserWallets_LastUpdated",
                table: "UserWallets",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_UserWallets_UsdBalance",
                table: "UserWallets",
                column: "UsdBalance");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedAt",
                table: "WalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Currency",
                table: "WalletTransactions",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_RelatedEntity",
                table: "WalletTransactions",
                columns: new[] { "RelatedEntityId", "RelatedEntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_RelatedEntityId",
                table: "WalletTransactions",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Type",
                table: "WalletTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserId",
                table: "WalletTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserId_CreatedAt",
                table: "WalletTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserId_Currency",
                table: "WalletTransactions",
                columns: new[] { "UserId", "Currency" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRequests");

            migrationBuilder.DropTable(
                name: "UserWallets");

            migrationBuilder.DropTable(
                name: "WalletTransactions");
        }
    }
}
