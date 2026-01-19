using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalorieTracker.data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CaloriesPer100g = table.Column<double>(type: "REAL", nullable: false),
                    ProteinPer100g = table.Column<double>(type: "REAL", nullable: false),
                    CarbsPer100g = table.Column<double>(type: "REAL", nullable: false),
                    FatPer100g = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    HeightCm = table.Column<double>(type: "REAL", nullable: false),
                    ActivityLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightGoal = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightChangeRateKgPerWeek = table.Column<double>(type: "REAL", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MealType = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    FoodId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountGrams = table.Column<double>(type: "REAL", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealEntries_Foods_FoodId",
                        column: x => x.FoodId,
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealEntries_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProteinPercentage = table.Column<double>(type: "REAL", nullable: false),
                    CarbsPercentage = table.Column<double>(type: "REAL", nullable: false),
                    FatPercentage = table.Column<double>(type: "REAL", nullable: false),
                    UseMetricSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: false),
                    TrackMacros = table.Column<bool>(type: "INTEGER", nullable: false),
                    TrackWater = table.Column<bool>(type: "INTEGER", nullable: false),
                    DietType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MealReminderTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EnableMealReminders = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightKg = table.Column<double>(type: "REAL", nullable: false),
                    LogDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserProfileId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightLogs_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeightLogs_UserProfiles_UserProfileId1",
                        column: x => x.UserProfileId1,
                        principalTable: "UserProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "ActivityLevel", "BirthDate", "CreatedDate", "Gender", "HeightCm", "LastUpdatedDate", "Name", "WeightChangeRateKgPerWeek", "WeightGoal" },
                values: new object[] { 1, 2, new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 19, 13, 47, 3, 224, DateTimeKind.Utc).AddTicks(2670), 2, 170.0, null, "Default User", 0.5, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_MealEntries_EntryDate",
                table: "MealEntries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_MealEntries_FoodId",
                table: "MealEntries",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_MealEntries_UserProfileId",
                table: "MealEntries",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserProfileId",
                table: "UserSettings",
                column: "UserProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightLogs_LogDate",
                table: "WeightLogs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_WeightLogs_UserProfileId",
                table: "WeightLogs",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightLogs_UserProfileId1",
                table: "WeightLogs",
                column: "UserProfileId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealEntries");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "WeightLogs");

            migrationBuilder.DropTable(
                name: "Foods");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
