using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_conversations_JobId",
                table: "conversations");

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCode",
                table: "users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationCodeExpiresAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_JobId_ProfessionalId",
                table: "conversations",
                columns: new[] { "JobId", "ProfessionalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_conversations_JobId_ProfessionalId",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeExpiresAt",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_JobId",
                table: "conversations",
                column: "JobId",
                unique: true);
        }
    }
}
