using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomChargeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DamagedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomChargeItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInspections_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomInspectionDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomInspectionId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    RoomChargeItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResultType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInspectionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInspectionDetails_RoomChargeItems_RoomChargeItemId",
                        column: x => x.RoomChargeItemId,
                        principalTable: "RoomChargeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomInspectionDetails_RoomInspections_RoomInspectionId",
                        column: x => x.RoomInspectionId,
                        principalTable: "RoomInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomInspectionDetails_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "RoomChargeItems",
                columns: new[] { "Id", "Category", "CreatedAt", "DamagedPrice", "IsDeleted", "LostPrice", "Name", "UpdatedAt", "UsedPrice" },
                values: new object[,]
                {
                    { 1, "MiniBar", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Rượu", null, 0m },
                    { 2, "MiniBar", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Nước", null, 0m },
                    { 3, "MiniBar", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Hoa quả", null, 0m },
                    { 4, "MiniBar", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Snack", null, 0m },
                    { 5, "Asset", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Khăn", null, 0m },
                    { 6, "Asset", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Ga", null, 0m },
                    { 7, "Asset", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Gối", null, 0m },
                    { 8, "Asset", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Áo choàng", null, 0m },
                    { 9, "Asset", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Máy sấy", null, 0m },
                    { 10, "Asset", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "TV", null, 0m },
                    { 11, "Asset", new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, false, 0m, "Điều hòa", null, 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomChargeItems_Category_Name",
                table: "RoomChargeItems",
                columns: new[] { "Category", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomInspectionDetails_RoomChargeItemId",
                table: "RoomInspectionDetails",
                column: "RoomChargeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInspectionDetails_RoomId",
                table: "RoomInspectionDetails",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInspectionDetails_RoomInspectionId",
                table: "RoomInspectionDetails",
                column: "RoomInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInspections_BookingId",
                table: "RoomInspections",
                column: "BookingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomInspectionDetails");

            migrationBuilder.DropTable(
                name: "RoomChargeItems");

            migrationBuilder.DropTable(
                name: "RoomInspections");
        }
    }
}
