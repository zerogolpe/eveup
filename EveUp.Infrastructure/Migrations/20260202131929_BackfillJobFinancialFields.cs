using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillJobFinancialFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill EveUpFee and NetFee for existing jobs
            migrationBuilder.Sql(@"
                UPDATE jobs
                SET ""EveUpFee"" = ""GrossFee"" * ""EveUpFeePercent"",
                    ""NetFee"" = ""GrossFee"" - (""GrossFee"" * ""EveUpFeePercent"")
                WHERE ""EveUpFee"" = 0 AND ""NetFee"" = 0 AND ""GrossFee"" > 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
