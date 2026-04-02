using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanningPoker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEditedProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "Users");
        }
    }
}
