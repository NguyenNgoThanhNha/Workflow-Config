using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowConfig.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActivityActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Actions",
                table: "Sys_Activity",
                type: "varchar(4)",
                nullable: false,
                defaultValue: "CRUD");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "Sys_Activity",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Actions",
                table: "Sys_Activity");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "Sys_Activity");
        }
    }
}
