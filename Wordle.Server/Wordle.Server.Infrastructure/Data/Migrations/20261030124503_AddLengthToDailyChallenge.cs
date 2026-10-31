using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordle.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLengthToDailyChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyChallenges_Date",
                table: "DailyChallenges");

            migrationBuilder.AddColumn<int>(
                name: "Length",
                table: "DailyChallenges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DailyChallenges_Date_Length",
                table: "DailyChallenges",
                columns: new[] { "Date", "Length" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyChallenges_Date_Length",
                table: "DailyChallenges");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "DailyChallenges");

            migrationBuilder.CreateIndex(
                name: "IX_DailyChallenges_Date",
                table: "DailyChallenges",
                column: "Date");
        }
    }
}
