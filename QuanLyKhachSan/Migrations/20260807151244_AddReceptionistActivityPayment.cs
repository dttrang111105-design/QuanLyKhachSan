using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionistActivityPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "ReceptionistActivities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionistActivities_PaymentId",
                table: "ReceptionistActivities",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceptionistActivities_Payments_PaymentId",
                table: "ReceptionistActivities",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceptionistActivities_Payments_PaymentId",
                table: "ReceptionistActivities");

            migrationBuilder.DropIndex(
                name: "IX_ReceptionistActivities_PaymentId",
                table: "ReceptionistActivities");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "ReceptionistActivities");
        }
    }
}
