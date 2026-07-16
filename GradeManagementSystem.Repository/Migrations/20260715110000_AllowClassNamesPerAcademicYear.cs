using GradeManagementSystem.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GradeDbContext))]
    [Migration("20260715110000_AllowClassNamesPerAcademicYear")]
    public partial class AllowClassNamesPerAcademicYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classes_ClassName",
                table: "Classes");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_AcademicYearID_DepartmentID_ClassName",
                table: "Classes",
                columns: new[] { "AcademicYearID", "DepartmentID", "ClassName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classes_AcademicYearID_DepartmentID_ClassName",
                table: "Classes");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassName",
                table: "Classes",
                column: "ClassName",
                unique: true);
        }
    }
}
