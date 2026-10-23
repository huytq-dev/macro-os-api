using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S27_BodyMetricUniqueDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GUARD: không sửa/xoá dữ liệu nào. Nếu còn hàng trùng (UserId, MeasuredDate, Method) chưa xoá thì DỪNG (cả migration rollback) với thông báo rõ thay vì lỗi tạo index khó hiểu; dọn trùng bằng quy trình đã duyệt TRƯỚC khi áp dụng
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM [BodyMetrics] WHERE [IsDeleted] = 0 GROUP BY [UserId], [MeasuredDate], [Method] HAVING COUNT(*) > 1)
    THROW 51027, 'S27: BodyMetrics has duplicate (UserId, MeasuredDate, Method) rows. Reconcile them first (Docs/Design/Verification/BodyMetric_UniqueDay_Migration_Proposal.md). Nothing was changed.', 1;");

            migrationBuilder.CreateIndex(
                name: "UX_BodyMetrics_UserId_MeasuredDate_Method",
                table: "BodyMetrics",
                columns: new[] { "UserId", "MeasuredDate", "Method" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BodyMetrics_UserId_MeasuredDate_Method",
                table: "BodyMetrics");
        }
    }
}
