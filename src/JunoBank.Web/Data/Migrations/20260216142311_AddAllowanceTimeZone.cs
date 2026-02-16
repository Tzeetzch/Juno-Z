using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JunoBank.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "ScheduledAllowances",
                type: "TEXT",
                nullable: false,
                defaultValue: "UTC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "ScheduledAllowances");
        }
    }
}
