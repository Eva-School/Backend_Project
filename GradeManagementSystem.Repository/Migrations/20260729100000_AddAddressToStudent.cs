using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(GradeManagementSystem.Repository.Data.GradeDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260729100000_AddAddressToStudent")]
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
