using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S26_ProgramWeekDayTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DaySnapshotRevision",
                table: "MacroProgramWeeks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MacroProgramWeekDayTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacroProgramWeekId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotRevision = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsFastingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Standard"),
                    Distribution = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdditionalTargetsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CaloriesTarget = table.Column<int>(type: "int", nullable: false),
                    ProteinTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CarbsTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    FatTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroProgramWeekDayTargets", x => x.Id);
                    table.CheckConstraint("CK_MacroProgramWeekDayTargets_Revision", "[SnapshotRevision] >= 1");
                    table.ForeignKey(
                        name: "FK_MacroProgramWeekDayTargets_MacroProgramWeeks_MacroProgramWeekId",
                        column: x => x.MacroProgramWeekId,
                        principalTable: "MacroProgramWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MacroProgramWeekDayTargets_MacroProgramWeekId_SnapshotRevision_DayOfWeek",
                table: "MacroProgramWeekDayTargets",
                columns: new[] { "MacroProgramWeekId", "SnapshotRevision", "DayOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // CẢNH BÁO MẤT DỮ LIỆU (D2): Down xoá bảng MacroProgramWeekDayTargets (toàn bộ snapshot 7 ngày + audit các lần ghi lại cùng ngày) và cột MacroProgramWeeks.DaySnapshotRevision kể từ khi migration được áp dụng.
            // Migration chỉ THÊM (không sửa/xoá dữ liệu cũ) nên Up an toàn; sau Down, lịch sử target ngày của các tuần đã chụp quay về fallback dòng ngày HIỆN TẠI (LEGACY_UNVERIFIED) và KHÔNG dựng lại được.
            // Nếu đã dùng thật, sao lưu bảng này trước khi chạy Down.
            migrationBuilder.DropTable(
                name: "MacroProgramWeekDayTargets");

            migrationBuilder.DropColumn(
                name: "DaySnapshotRevision",
                table: "MacroProgramWeeks");
        }
    }
}
