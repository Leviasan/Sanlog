using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Leviasan.Example.WebApplication.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogApps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Application = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogApps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "varchar(43)", unicode: false, maxLength: 43, nullable: true),
                    LogLevelId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_LogApps_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "LogApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogEntries_LogLevels_LogLevelId",
                        column: x => x.LogLevelId,
                        principalTable: "LogLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HResult = table.Column<int>(type: "int", nullable: false),
                    StackTrace = table.Column<string>(type: "varchar(max)", unicode: false, maxLength: 2147483647, nullable: true),
                    Source = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    HelpLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetSite = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    LogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentExceptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogErrors_LogEntries_LogEntryId",
                        column: x => x.LogEntryId,
                        principalTable: "LogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogErrors_LogErrors_ParentExceptionId",
                        column: x => x.ParentExceptionId,
                        principalTable: "LogErrors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LogScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    LogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogScopes_LogEntries_LogEntryId",
                        column: x => x.LogEntryId,
                        principalTable: "LogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LogLevels",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 0, "Trace" },
                    { 1, "Debug" },
                    { 2, "Information" },
                    { 3, "Warning" },
                    { 4, "Error" },
                    { 5, "Critical" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogApps_Application_Environment",
                table: "LogApps",
                columns: new[] { "Application", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_ApplicationId",
                table: "LogEntries",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_DateTime",
                table: "LogEntries",
                column: "DateTime");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_LogLevelId",
                table: "LogEntries",
                column: "LogLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_LogErrors_LogEntryId",
                table: "LogErrors",
                column: "LogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LogErrors_ParentExceptionId",
                table: "LogErrors",
                column: "ParentExceptionId");

            migrationBuilder.CreateIndex(
                name: "IX_LogLevels_Name",
                table: "LogLevels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogScopes_LogEntryId",
                table: "LogScopes",
                column: "LogEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogErrors");

            migrationBuilder.DropTable(
                name: "LogScopes");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "LogApps");

            migrationBuilder.DropTable(
                name: "LogLevels");
        }
    }
}
