using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanningPoker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentTask_ForceUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentTaskName",
                table: "Rooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentTaskName",
                table: "Rooms");
        }
    }
}
