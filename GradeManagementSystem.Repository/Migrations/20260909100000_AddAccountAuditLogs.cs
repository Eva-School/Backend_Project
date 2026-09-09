using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradeManagementSystem.Repository.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(GradeManagementSystem.Repository.Data.GradeDbContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260909100000_AddAccountAuditLogs")]
    public partial class AddAccountAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountAuditLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    ActorUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetUserId = table.Column<int>(type: "integer", nullable: false),
                    TargetUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAuditLogs", x => x.LogID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAuditLogs_TargetUserId",
                table: "AccountAuditLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountAuditLogs_Timestamp",
                table: "AccountAuditLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAuditLogs");
        }
    }
}
