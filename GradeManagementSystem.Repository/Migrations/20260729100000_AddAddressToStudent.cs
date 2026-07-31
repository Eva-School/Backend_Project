using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Students",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Students");
        }
    }
}
