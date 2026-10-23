namespace Application;

/// <summary>
/// Quy tắc MỘT ngày MỘT giá trị cho số đo vòng (hàng BodyMetric Tape) khi DB có thể chứa nhiều hàng cùng ngày (index (UserId, MeasuredDate) KHÔNG unique, dữ liệu cũ/đua ghi có thể trùng). Hàm thuần.
/// ĐỌC: giá trị của một số đo trong ngày = hàng MỚI NHẤT (CreatedAt) CÓ giá trị đó. GHI phải nhất quán với ĐỌC: ghi vào hàng mới nhất; xoá xoá ở MỌI hàng của ngày (nếu chỉ xoá hàng được chọn thì giá trị
/// của hàng trùng cũ hơn hiện lại như một điểm ma). Trước đây ghi chọn hàng CŨ NHẤT còn đọc lấy hàng MỚI NHẤT nên sửa/xoá trên ngày trùng không có tác dụng đúng.
/// </summary>
public static class TapeDayRows
{
    /// <summary>Mỗi ngày một giá trị của <paramref name="m"/> (mới nhất trước): hàng mới nhất có giá trị thắng. Hàng không có giá trị số đo đó bị bỏ qua (không tạo điểm).</summary>
    public static List<MeasurementEntryModel> Collapse(IEnumerable<BodyMetric> tapeRows, BodyMeasurement m)
        => tapeRows.Where(r => m.Get(r) is not null)
            .OrderByDescending(r => r.MeasuredDate).ThenByDescending(r => r.CreatedAt)
            .GroupBy(r => r.MeasuredDate)
            .Select(g => new MeasurementEntryModel(g.Key, m.Get(g.First())!.Value)).ToList();

    /// <summary>
    /// Áp một thay đổi của MỘT số đo lên các hàng Tape của MỘT ngày (<paramref name="rowsNewestFirst"/>, mới nhất trước; có thể rỗng). Có giá trị: ghi vào hàng mới nhất (tạo hàng nếu chưa có qua <paramref name="create"/>);
    /// null: xoá số đo đó ở MỌI hàng của ngày. Trả (Changed, hàng mới tạo). Hàng hết ô nào gọi <see cref="Emptied"/> sau khi áp xong mọi thay đổi.
    /// </summary>
    public static (bool Changed, List<BodyMetric> Added) Apply(List<BodyMetric> rowsNewestFirst, BodyMeasurement m, decimal? value, Func<BodyMetric> create)
    {
        var added = new List<BodyMetric>();
        var changed = false;
        if (value is { } cm)
        {
            var row = rowsNewestFirst.FirstOrDefault();
            if (row is null)
            {
                row = create();
                rowsNewestFirst.Add(row);
                added.Add(row);
                changed = true;
            }
            if (m.Get(row) != cm) { m.Set(row, cm); changed = true; }
        }
        else
            foreach (var r in rowsNewestFirst.Where(r => m.Get(r) is not null))
            {
                m.Set(r, null);
                changed = true;
            }
        return (changed, added);
    }

    /// <summary>Các hàng không còn ô số đo nào (bỏ hẳn: ngày đó không còn số đo vòng ở hàng đó).</summary>
    public static List<BodyMetric> Emptied(IEnumerable<BodyMetric> rows) => rows.Where(r => !BodyMeasurements.HasAny(r)).ToList();
}
