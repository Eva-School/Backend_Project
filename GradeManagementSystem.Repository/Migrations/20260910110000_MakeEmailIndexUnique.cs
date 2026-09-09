using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(GradeManagementSystem.Repository.Data.GradeDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260910110000_MakeEmailIndexUnique")]
    public partial class MakeEmailIndexUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
