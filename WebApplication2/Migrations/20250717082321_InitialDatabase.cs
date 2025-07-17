using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "LogTenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ClientDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogTenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogApps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Application = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogApps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogApps_LogTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "LogTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<string>(type: "varchar(43)", unicode: false, maxLength: 43, nullable: true),
                    LoggingLevelId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_LogApps_AppId",
                        column: x => x.AppId,
                        principalTable: "LogApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogEntries_LogLevels_LoggingLevelId",
                        column: x => x.LoggingLevelId,
                        principalTable: "LogLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HResult = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    StackTrace = table.Column<string>(type: "varchar(max)", unicode: false, maxLength: 2147483647, nullable: true),
                    Source = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    HelpLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetSite = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    LogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentExceptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    Message = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    LogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "IX_LogApps_Application_Environment_TenantId",
                table: "LogApps",
                columns: new[] { "Application", "Environment", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogApps_TenantId",
                table: "LogApps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_AppId",
                table: "LogEntries",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_LoggingLevelId",
                table: "LogEntries",
                column: "LoggingLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Timestamp",
                table: "LogEntries",
                column: "Timestamp",
                descending: new bool[0]);

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

            migrationBuilder.DropTable(
                name: "LogTenants");
        }
    }
}
