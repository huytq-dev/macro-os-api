using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>
    /// S29: Manual Nutrition của ngày = 4 cột nullable trên FoodLogs (kcal, protein, carbs, fat). Additive: không default, không backfill, không đụng FoodLogEntries, không chuyển đổi Quick Add cũ;
    /// dòng cũ có 4 cột = NULL (không có override) nên thoả CHECK. CHẠY CHO DB DÙNG CHUNG/PRODUCTION CẦN PHÊ DUYỆT RIÊNG (xem NDM_S29_Design_And_P0_Report.md).
    /// <para>
    /// <b>Down GÂY MẤT DỮ LIỆU</b>: DropColumn xoá mọi tổng nhập tay đã lưu sau khi S29 được áp dụng (không có bản sao nào khác). Trước khi rollback ở bất kỳ DB nào có dữ liệu thật,
    /// phải xuất 4 cột ra bảng/tệp sao lưu (SELECT Id, UserId, LogDate, Manual* FROM FoodLogs WHERE ManualCaloriesKcal IS NOT NULL OR …) rồi mới Down. Không tự rollback trên shared/production.
    /// </para>
    /// </summary>
    public partial class S29_ManualNutrition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ManualCaloriesKcal",
                table: "FoodLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManualCarbsG",
                table: "FoodLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManualFatG",
                table: "FoodLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManualProteinG",
                table: "FoodLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_FoodLogs_ManualNutrition_Range",
                table: "FoodLogs",
                sql: "([ManualCaloriesKcal] IS NULL OR ([ManualCaloriesKcal] >= 0 AND [ManualCaloriesKcal] <= 10000)) AND ([ManualProteinG] IS NULL OR ([ManualProteinG] >= 0 AND [ManualProteinG] <= 1000)) AND ([ManualCarbsG] IS NULL OR ([ManualCarbsG] >= 0 AND [ManualCarbsG] <= 1000)) AND ([ManualFatG] IS NULL OR ([ManualFatG] >= 0 AND [ManualFatG] <= 1000))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // TỪ CHỐI khi còn dữ liệu manual: Down không bao giờ âm thầm Drop 4 cột đang chứa giá trị người dùng nhập. THROW huỷ cả migration (SQL Server chạy migration trong một transaction) nên DB ở nguyên S29.
            // Cách đi đúng: xuất Id, UserId, LogDate + 4 cột Manual ra bảng/tệp sao lưu, rồi CHỦ ĐỘNG đặt chúng về NULL (UPDATE) và chạy Down lại (xem S29_Production_Readiness.md).
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM [FoodLogs] WHERE [ManualCaloriesKcal] IS NOT NULL OR [ManualProteinG] IS NOT NULL OR [ManualCarbsG] IS NOT NULL OR [ManualFatG] IS NOT NULL) " +
                "THROW 51029, N'S29 Down refused: FoodLogs still holds Manual Nutrition values. Export Id, UserId, LogDate and the four Manual columns to a backup first, then clear them on purpose (UPDATE FoodLogs SET ManualCaloriesKcal = NULL, ManualProteinG = NULL, ManualCarbsG = NULL, ManualFatG = NULL) and run Down again.', 1;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FoodLogs_ManualNutrition_Range",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "ManualCaloriesKcal",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "ManualCarbsG",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "ManualFatG",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "ManualProteinG",
                table: "FoodLogs");
        }
    }
}
