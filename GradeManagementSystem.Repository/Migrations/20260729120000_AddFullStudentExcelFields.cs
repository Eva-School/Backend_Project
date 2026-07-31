using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(GradeManagementSystem.Repository.Data.GradeDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260729120000_AddFullStudentExcelFields")]
    public partial class AddFullStudentExcelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentCode",
                table: "Students",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameArabic",
                table: "Students",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEnglish",
                table: "Students",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Students",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfBirth",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressArabic",
                table: "Students",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Students",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Governorate",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherName",
                table: "Students",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherPhone",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherProfession",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherName",
                table: "Students",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherPhone",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherProfession",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelativeName",
                table: "Students",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelativePhone",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Religion",
                table: "Students",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentPhone",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthProblems",
                table: "Students",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingDocumentation",
                table: "Students",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DocumentsDelivered",
                table: "Students",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PreparatoryGrade",
                table: "Students",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FeesPaid",
                table: "Students",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "StudentCode", table: "Students");
            migrationBuilder.DropColumn(name: "NameArabic", table: "Students");
            migrationBuilder.DropColumn(name: "NameEnglish", table: "Students");
            migrationBuilder.DropColumn(name: "Nationality", table: "Students");
            migrationBuilder.DropColumn(name: "DateOfBirth", table: "Students");
            migrationBuilder.DropColumn(name: "PlaceOfBirth", table: "Students");
            migrationBuilder.DropColumn(name: "AddressArabic", table: "Students");
            migrationBuilder.DropColumn(name: "Email", table: "Students");
            migrationBuilder.DropColumn(name: "Governorate", table: "Students");
            migrationBuilder.DropColumn(name: "FatherName", table: "Students");
            migrationBuilder.DropColumn(name: "FatherPhone", table: "Students");
            migrationBuilder.DropColumn(name: "FatherProfession", table: "Students");
            migrationBuilder.DropColumn(name: "MotherName", table: "Students");
            migrationBuilder.DropColumn(name: "MotherPhone", table: "Students");
            migrationBuilder.DropColumn(name: "MotherProfession", table: "Students");
            migrationBuilder.DropColumn(name: "RelativeName", table: "Students");
            migrationBuilder.DropColumn(name: "RelativePhone", table: "Students");
            migrationBuilder.DropColumn(name: "Religion", table: "Students");
            migrationBuilder.DropColumn(name: "StudentPhone", table: "Students");
            migrationBuilder.DropColumn(name: "HealthProblems", table: "Students");
            migrationBuilder.DropColumn(name: "MissingDocumentation", table: "Students");
            migrationBuilder.DropColumn(name: "DocumentsDelivered", table: "Students");
            migrationBuilder.DropColumn(name: "PreparatoryGrade", table: "Students");
            migrationBuilder.DropColumn(name: "FeesPaid", table: "Students");
        }
    }
}
