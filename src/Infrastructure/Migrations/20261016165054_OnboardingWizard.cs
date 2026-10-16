using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OnboardingWizard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliateCode",
                table: "UserProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyFat",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardioExperience",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DailyActivity",
                table: "UserProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExerciseSessions",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HealthDisclaimerAcceptedAt",
                table: "UserProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiftingExperience",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PrivacyNoticeAcceptedAt",
                table: "UserProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecentWeightTrend",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralSource",
                table: "UserProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeighedMoreThanBefore",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffiliateCode",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BodyFat",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CardioExperience",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DailyActivity",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ExerciseSessions",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "HealthDisclaimerAcceptedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LiftingExperience",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "PrivacyNoticeAcceptedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RecentWeightTrend",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ReferralSource",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeighedMoreThanBefore",
                table: "UserProfiles");
        }
    }
}
