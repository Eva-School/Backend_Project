using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(GradeManagementSystem.Repository.Data.GradeDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260729110000_AddQuizzesTables")]
    public partial class AddQuizzesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    QuizID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuizDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClassID = table.Column<int>(type: "integer", nullable: false),
                    SubjectID = table.Column<int>(type: "integer", nullable: false),
                    AcademicYearID = table.Column<int>(type: "integer", nullable: false),
                    CreatedByTeacherID = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.QuizID);
                    table.ForeignKey(
                        name: "FK_Quizzes_AcademicYears_AcademicYearID",
                        column: x => x.AcademicYearID,
                        principalTable: "AcademicYears",
                        principalColumn: "AcademicYearID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quizzes_Classes_ClassID",
                        column: x => x.ClassID,
                        principalTable: "Classes",
                        principalColumn: "ClassID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quizzes_Subjects_SubjectID",
                        column: x => x.SubjectID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quizzes_Teachers_CreatedByTeacherID",
                        column: x => x.CreatedByTeacherID,
                        principalTable: "Teachers",
                        principalColumn: "TeacherID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuizGrades",
                columns: table => new
                {
                    QuizGradeID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizID = table.Column<int>(type: "integer", nullable: false),
                    StudentID = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    GradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizGrades", x => x.QuizGradeID);
                    table.ForeignKey(
                        name: "FK_QuizGrades_Quizzes_QuizID",
                        column: x => x.QuizID,
                        principalTable: "Quizzes",
                        principalColumn: "QuizID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizGrades_Students_StudentID",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizGrades_QuizID_StudentID",
                table: "QuizGrades",
                columns: new[] { "QuizID", "StudentID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizGrades_StudentID",
                table: "QuizGrades",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_AcademicYearID",
                table: "Quizzes",
                column: "AcademicYearID");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ClassID",
                table: "Quizzes",
                column: "ClassID");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CreatedByTeacherID",
                table: "Quizzes",
                column: "CreatedByTeacherID");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_SubjectID",
                table: "Quizzes",
                column: "SubjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizGrades");

            migrationBuilder.DropTable(
                name: "Quizzes");
        }
    }
}
