using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JunoBank.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DayOfMonth",
                table: "ScheduledAllowances",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "ScheduledAllowances",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MonthOfYear",
                table: "ScheduledAllowances",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayOfMonth",
                table: "ScheduledAllowances");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "ScheduledAllowances");

            migrationBuilder.DropColumn(
                name: "MonthOfYear",
                table: "ScheduledAllowances");
        }
    }
}
