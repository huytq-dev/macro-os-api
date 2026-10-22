using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S25_ExpenditureSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AthleticPursuits",
                table: "UserProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeighedMoreThanBeforeAtWeightKg",
                table: "UserProfiles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeighedMoreThanBeforeThresholdKg",
                table: "UserProfiles",
                type: "decimal(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeighedMoreThanBeforeThresholdShown",
                table: "UserProfiles",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeighedMoreThanBeforeThresholdUnit",
                table: "UserProfiles",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalculationRevision",
                table: "ExpenditureSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenditureModePeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StaticTdeeKcal = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureModePeriods", x => x.Id);
                    table.CheckConstraint("CK_ExpenditureModePeriods_Static", "([Mode] = 'Static' AND [StaticTdeeKcal] IS NOT NULL AND [StaticTdeeKcal] > 0) OR ([Mode] = 'Dynamic' AND [StaticTdeeKcal] IS NULL)");
                    table.ForeignKey(
                        name: "FK_ExpenditureModePeriods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExpenditureSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitialEstimateMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ManualInitialTdeeKcal = table.Column<int>(type: "int", nullable: true),
                    BmrEquation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "MacroFactor"),
                    BmrSex = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DefaultCalculationStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CalculationStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CalculationRevision = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExpenditureSettings", x => x.Id);
                    table.CheckConstraint("CK_UserExpenditureSettings_ManualTdee", "([InitialEstimateMode] = 'Manual' AND [ManualInitialTdeeKcal] IS NOT NULL AND [ManualInitialTdeeKcal] > 0) OR (ISNULL([InitialEstimateMode], '') <> 'Manual' AND [ManualInitialTdeeKcal] IS NULL)");
                    table.CheckConstraint("CK_UserExpenditureSettings_Revision", "[CalculationRevision] >= 0");
                    table.ForeignKey(
                        name: "FK_UserExpenditureSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureModePeriods_UserId_EffectiveFrom",
                table: "ExpenditureModePeriods",
                columns: new[] { "UserId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExpenditureSettings_UserId",
                table: "UserExpenditureSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // CẢNH BÁO MẤT DỮ LIỆU: Down xoá bảng UserExpenditureSettings (gồm BmrSex, DefaultCalculationStartDate, CalculationStartDate, revision), ExpenditureModePeriods và sáu cột
            // UserProfiles.AthleticPursuits, WeighedMoreThanBeforeAtWeightKg, WeighedMoreThanBeforeThresholdKg, WeighedMoreThanBeforeThresholdShown, WeighedMoreThanBeforeThresholdUnit, ExpenditureSnapshots.CalculationRevision
            // cùng TOÀN BỘ dữ liệu user đã ghi vào đó kể từ khi migration được áp dụng (cài đặt expenditure, ngày bắt đầu mặc định, lịch sử Static/Dynamic, đáp án Athletic Pursuits,
            // cân + ngưỡng chính xác + số hiển thị + đơn vị của câu hỏi Prior Weight lúc trả lời, revision). Không có dữ liệu cũ nào bị mất (migration chỉ thêm).
            // Nếu đã dùng thật, sao lưu các bảng/cột này trước khi chạy Down. Ngưỡng/ngày bắt đầu gốc KHÔNG dựng lại được sau khi mất.
            migrationBuilder.DropTable(
                name: "ExpenditureModePeriods");

            migrationBuilder.DropTable(
                name: "UserExpenditureSettings");

            migrationBuilder.DropColumn(
                name: "AthleticPursuits",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeighedMoreThanBeforeAtWeightKg",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeighedMoreThanBeforeThresholdKg",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeighedMoreThanBeforeThresholdShown",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeighedMoreThanBeforeThresholdUnit",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CalculationRevision",
                table: "ExpenditureSnapshots");
        }
    }
}
