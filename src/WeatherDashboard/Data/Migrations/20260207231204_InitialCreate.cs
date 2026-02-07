using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WeatherDashboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false),
                    FeelsLike = table.Column<double>(type: "REAL", nullable: false),
                    Humidity = table.Column<double>(type: "REAL", nullable: false),
                    Pressure = table.Column<double>(type: "REAL", nullable: false),
                    WindSpeed = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IconCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherRecords_SavedLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "SavedLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "UserSettings",
                columns: new[] { "Id", "Key", "LastModified", "Value" },
                values: new object[,]
                {
                    { 1, "TemperatureUnit", new DateTime(2026, 2, 7, 16, 12, 4, 516, DateTimeKind.Local).AddTicks(2154), "Celsius" },
                    { 2, "RefreshInterval", new DateTime(2026, 2, 7, 16, 12, 4, 516, DateTimeKind.Local).AddTicks(2212), "30" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedLocations_Name",
                table: "SavedLocations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_Key",
                table: "UserSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherRecords_LocationId_Timestamp",
                table: "WeatherRecords",
                columns: new[] { "LocationId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "WeatherRecords");

            migrationBuilder.DropTable(
                name: "SavedLocations");
        }
    }
}
