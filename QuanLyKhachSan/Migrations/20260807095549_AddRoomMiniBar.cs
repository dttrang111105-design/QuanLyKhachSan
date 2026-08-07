using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomMiniBar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomMiniBarItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    RoomChargeItemId = table.Column<int>(type: "int", nullable: false),
                    StandardQuantity = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomMiniBarItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomMiniBarItems_RoomChargeItems_RoomChargeItemId",
                        column: x => x.RoomChargeItemId,
                        principalTable: "RoomChargeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomMiniBarItems_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomMiniBarItems_RoomChargeItemId",
                table: "RoomMiniBarItems",
                column: "RoomChargeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomMiniBarItems_RoomId_RoomChargeItemId",
                table: "RoomMiniBarItems",
                columns: new[] { "RoomId", "RoomChargeItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomMiniBarItems");
        }
    }
}
