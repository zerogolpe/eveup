using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_5_SecurityCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_denunciations_users_InitiatorId",
                table: "denunciations");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "users",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "payments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<decimal>(
                name: "EveUpFee",
                table: "jobs",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetFee",
                table: "jobs",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "jobs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "denunciations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "EveUpFee",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "NetFee",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "denunciations");

            migrationBuilder.AddForeignKey(
                name: "FK_denunciations_users_InitiatorId",
                table: "denunciations",
                column: "InitiatorId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
