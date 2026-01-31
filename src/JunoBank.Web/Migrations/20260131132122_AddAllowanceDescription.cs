using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JunoBank.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ScheduledAllowances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ScheduledAllowances");
        }
    }
}
