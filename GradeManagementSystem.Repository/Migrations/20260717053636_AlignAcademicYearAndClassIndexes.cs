using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AlignAcademicYearAndClassIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classes_AcademicYearID",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_AcademicYearID_DepartmentID_ClassName",
                table: "Classes");

            migrationBuilder.AlterColumn<string>(
                name: "Stage",
                table: "AcademicYears",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_AcademicYearID_DepartmentID_ClassName",
                table: "Classes",
                columns: new[] { "AcademicYearID", "DepartmentID", "ClassName" },
                unique: true,
                filter: "[AcademicYearID] IS NOT NULL AND [DepartmentID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_YearName_Stage",
                table: "AcademicYears",
                columns: new[] { "YearName", "Stage" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classes_AcademicYearID_DepartmentID_ClassName",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_AcademicYears_YearName_Stage",
                table: "AcademicYears");

            migrationBuilder.AlterColumn<string>(
                name: "Stage",
                table: "AcademicYears",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_AcademicYearID",
                table: "Classes",
                column: "AcademicYearID");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_AcademicYearID_DepartmentID_ClassName",
                table: "Classes",
                columns: new[] { "AcademicYearID", "DepartmentID", "ClassName" },
                unique: true);
        }
    }
}
