namespace Application;

/// <summary>
/// Hàng đợi bền dựng lại dữ liệu dẫn xuất trên bảng <see cref="RecalculationJob"/> có sẵn (không cần migration): xếp job, chạy job đã nhận dưới khoá user, tính trạng thái cho FE.
/// Bất biến: (1) mỗi user có tối đa một lần dựng tại một thời điểm (khoá <see cref="DerivedDataSync.LockResource"/>); (2) job đọc dữ liệu nguồn SAU khi giữ khoá nên job cũ đến trễ vẫn tính từ nguồn mới nhất
/// và không đè kết quả mới; (3) snapshot do phiên bản thuật toán mới hơn dựng thì không bị ghi đè (xem <see cref="ExpenditureRebuilder"/>).
/// </summary>
public static class DerivedDataJobs
{
    private const int MaxErrorLength = 1900;
    private const int MaxAggregateDays = 400;   // kích thước lô khi làm mới aggregate của job FoodLogChanged (không còn là trần: cả khoảng được làm mới)

    /// <summary>
    /// Xếp job Pending cho user: gộp vào job Pending đang có (khoảng ngày hợp lại) nếu có, không thì tạo mới. Lưu ngay. <paramref name="error"/> là lý do (lỗi dựng đồng bộ) nếu có.
    /// FromDate = ngày đầu cần dựng lại snapshot; ToDate = ngày cuối của dữ liệu đã sửa (dùng làm mới aggregate dinh dưỡng với job FoodLogChanged).
    /// </summary>
    public static async Task EnqueueAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, RecalculationCause cause, string? error, CancellationToken ct, TimeProvider? tp = null)
    {
        // Thay đổi đang track của caller (snapshot của lần dựng đồng bộ ở đường ResumeFrom, job Failed của Retry) lưu TRƯỚC, như trước đây vẫn lưu cùng lúc; phần job bên dưới
        // nhờ vậy là câu lệnh riêng, thử lại được an toàn khi đua với một lần xếp job khác
        await uow.SaveChangesAsync(ct);
        var repo = uow.Repository<RecalculationJob>();
        if (to < from) to = from;
        var err = Trim(error);
        var isFood = cause == RecalculationCause.FoodLogChanged;

        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                // Đọc cũng nằm trong vòng thử lại: lỗi tạm thời/deadlock khi đọc sau khi nguồn đã lưu không được thành 500 mà không có job
                var pending = await repo.Query().Where(j => j.UserId == userId && j.State == JobState.Pending)
                    .OrderBy(j => j.FromDate).ThenBy(j => j.Id)
                    .Select(j => new { j.Id, j.FromDate, j.ToDate }).ToListAsync(ct);
                // Đích gộp: job Pending ĐÃ phủ khoảng mới (khoảng không đổi ⇒ không thể đụng unique (UserId, FromDate, ToDate) khi Pending), không thì job bắt đầu sớm nhất.
                // Một user có thể có vài job Pending (worker trả job về Pending khi Busy/huỷ); trước đây gộp vào job bất kỳ có thể tạo đúng khoảng của job kia ⇒ vi phạm unique ⇒ 500 sau khi nguồn đã lưu.
                var target = pending.FirstOrDefault(j => j.FromDate <= from && j.ToDate >= to) ?? pending.FirstOrDefault();
                if (target is null)
                {
                    await repo.AddAsync(new RecalculationJob
                    {
                        Id = Guid.NewGuid(), UserId = userId, Cause = cause, FromDate = from, ToDate = to, State = JobState.Pending,
                        AlgorithmVersion = AlgorithmVersions.Expenditure, LastError = err, // nhãn thực được ghi lại khi job chạy xong
                    }, ct);
                    await uow.SaveChangesAsync(ct);
                    return;
                }

                // Gộp NGUYÊN TỬ trong SQL (min/max trên giá trị HIỆN TẠI của dòng, dưới khoá dòng): hai lần xếp job đồng thời không ghi đè khoảng/nguyên nhân của nhau.
                // Chỉ khi còn Pending, và đóng dấu ModifiedAt: worker chỉ đánh dấu xong job nó đã đọc nếu job không đổi từ lúc đọc (RunClaimedAsync) ⇒ phần gộp thêm ở lại Pending cho lượt sau.
                // Job FoodLogChanged còn phải làm mới aggregate dinh dưỡng, nên sửa log ăn gộp vào job khác nguyên nhân ⇒ job mang FoodLogChanged (làm mới thêm vài ngày là vô hại, idempotent).
                var stamp = (tp ?? TimeProvider.System).GetUtcNow();
                var merged = await repo.Query().Where(j => j.Id == target.Id && j.State == JobState.Pending)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(j => j.FromDate, j => j.FromDate < from ? j.FromDate : from)
                        .SetProperty(j => j.ToDate, j => j.ToDate > to ? j.ToDate : to)
                        .SetProperty(j => j.Cause, j => isFood ? RecalculationCause.FoodLogChanged : j.Cause)
                        .SetProperty(j => j.LastError, j => err ?? j.LastError)
                        .SetProperty(j => j.ModifiedAt, stamp), ct);
                if (merged == 1) return;
                // 0 dòng: job vừa rời Pending (worker vừa đánh dấu xong) ⇒ đọc lại; lượt sau gộp vào job khác hoặc tạo job mới
            }
            // Đua với một lần xếp job khác (unique khi Pending), deadlock, lỗi mạng tạm thời: bỏ job chưa lưu, đọc lại. Transaction của caller đã bị server kết thúc (deadlock victim,
            // KILL, mất kết nối; lỗi có thể được EF bọc hoặc không) thì không thử lại: lệnh sau sẽ ghi ngoài nó ⇒ ném ra để caller rollback (DerivedDataSync xếp lại ngoài transaction)
            catch (Exception ex) when (attempt < 3 && IsDbFailure(ex) && !uow.IsTransactionBroken)
            {
                uow.DiscardChanges();
            }
        }
        throw new InvalidOperationException($"Could not enqueue a derived-data job for user {userId} after 4 attempts.");
    }

    /// <summary>
    /// Chạy job worker đã nhận (State = Running): lấy khoá user, ĐỌC LẠI nguồn dưới khoá, gộp mọi job Pending của user vào một lần dựng từ ngày sớm nhất đến hôm nay, rồi đánh dấu tất cả Succeeded
    /// trong cùng transaction với snapshot. Lỗi thì ném ra: caller rollback và ghi Failed cho job đã nhận.
    /// </summary>
    public static async Task<JobRunOutcome> RunClaimedAsync(IUnitOfWork uow, Guid userId, Guid claimedJobId, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        if (!await uow.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(userId), opt.DerivedWorkerLockWaitMs, ct))
        {
            await uow.RollbackTransactionAsync(ct);
            return JobRunOutcome.Busy;
        }

        var jobs = await uow.Repository<RecalculationJob>().Query(tracking: true)
            .Where(j => j.UserId == userId && (j.Id == claimedJobId || j.State == JobState.Pending))
            .ToListAsync(ct);
        if (jobs.Count > 0)
        {
            var from = jobs.Min(j => j.FromDate);

            // Aggregate dinh dưỡng của các ngày job FoodLogChanged đã sửa (bình thường đã làm đồng bộ; đây là đường bù khi đồng bộ lỗi). Cả khoảng FromDate..ToDate, theo lô ≤ MaxAggregateDays
            // ngày (RefreshAsync truy vấn theo khoảng của lô): job gộp có thể bắt đầu rất sớm (sửa cân cũ, khoảng trống program) mà ngày ăn vừa sửa nằm ở CUỐI; trước đây chỉ lấy 400 ngày đầu ⇒ bỏ sót
            var dates = jobs.Where(j => j.Cause == RecalculationCause.FoodLogChanged)
                .SelectMany(j => Enumerable.Range(0, j.ToDate.DayNumber - j.FromDate.DayNumber + 1).Select(j.FromDate.AddDays))
                .Distinct().Order().ToList();
            foreach (var chunk in dates.Chunk(MaxAggregateDays)) await NutritionAggregates.RefreshAsync(uow, userId, chunk, tp, ct);

            var localToday = (await UserLocale.GetAsync(uow, userId, tp, ct)).Today;
            await ExpenditureRebuilder.RebuildAsync(uow, userId, from, localToday, opt, ct, null, localToday);

            await uow.SaveChangesAsync(ct);   // snapshot + aggregate của lần dựng

            // Đánh dấu xong MỌI job đã đọc vào lần dựng này (job đã nhận + job Pending được gộp) CHỈ KHI từ lúc đọc job không đổi: EnqueueAsync đóng dấu ModifiedAt mỗi lần gộp thêm
            // (kể cả gộp không đổi khoảng); worker khác giành lại job quá lease rồi trả về Pending đổi State. Job đã đổi ⇒ để nguyên cho lượt sau: phần mới có thể đã ghi nguồn SAU khi
            // lần dựng này đọc. Job bị giành lại mà nội dung không đổi (vẫn Running) thì đánh dấu xong là đúng: lần dựng này đã đọc nguồn dưới khoá; worker kia chạy lại vô hại.
            // Trước đây job đã nhận được đánh dấu theo Id ⇒ phần gộp vào nó sau khi bị giành lại + trả về Pending bị nuốt mà trạng thái vẫn Ready.
            var now = tp.GetUtcNow();
            var version = AlgorithmVersions.ExpenditureFor(opt);
            foreach (var j in jobs)
            {
                var (s0, f, t, c, m) = (j.State, j.FromDate, j.ToDate, j.Cause, j.ModifiedAt);
                await uow.Repository<RecalculationJob>().Query()
                    .Where(x => x.Id == j.Id && x.State == s0 && x.FromDate == f && x.ToDate == t && x.Cause == c && x.ModifiedAt == m)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.State, JobState.Succeeded)
                        .SetProperty(x => x.FinishedAt, now)
                        .SetProperty(x => x.LastError, (string?)null)
                        .SetProperty(x => x.AlgorithmVersion, version), ct);
            }
        }
        await uow.CommitTransactionAsync(ct);
        return JobRunOutcome.Done;
    }

    /// <summary>Trạng thái dựng lại của user cho FE: Ready (không còn job), Updating (còn job Pending/Running hoặc Failed còn lượt thử), Failed (có job hết lượt thử).</summary>
    public static async Task<DerivedDataStatusResponse> StatusAsync(IUnitOfWork uow, Guid userId, AlgorithmOptions opt, CancellationToken ct)
    {
        var open = await uow.Repository<RecalculationJob>().Query()
            .Where(j => j.UserId == userId && j.State != JobState.Succeeded)
            .ToListAsync(ct);
        if (open.Count == 0)
        {
            var last = await uow.Repository<RecalculationJob>().Query()
                .Where(j => j.UserId == userId && j.State == JobState.Succeeded)
                .OrderByDescending(j => j.FinishedAt)
                .Select(j => j.FinishedAt)
                .FirstOrDefaultAsync(ct);
            return new DerivedDataStatusResponse(DerivedDataStatusResponse.Ready, null, null, 0, last);
        }

        return new DerivedDataStatusResponse(
            StateOf(open.Select(j => (j.State, j.AttemptCount)), opt.DerivedJobMaxAttempts),
            open.Min(j => j.FromDate), open.Max(j => j.ToDate), open.Max(j => j.AttemptCount),
            open.Max(j => j.FinishedAt ?? j.StartedAt ?? j.ModifiedAt ?? j.CreatedAt));
    }

    /// <summary>Quy tắc trạng thái (hàm thuần, nguồn duy nhất): không job mở = Ready; có job hết lượt thử (Failed, attempt ≥ tối đa) = Failed; còn lại = Updating.</summary>
    public static string StateOf(IEnumerable<(JobState State, int AttemptCount)> openJobs, int maxAttempts)
    {
        var any = false;
        foreach (var (state, attempts) in openJobs)
        {
            any = true;
            if (state == JobState.Failed && attempts >= maxAttempts) return DerivedDataStatusResponse.Failed;
        }
        return any ? DerivedDataStatusResponse.Updating : DerivedDataStatusResponse.Ready;
    }

    /// <summary>
    /// Chỉ trạng thái (Ready | Updating | Failed) bằng MỘT truy vấn: cùng giá trị <see cref="StatusAsync"/>.State nhưng không đọc "lần xong cuối" (chỉ để hiển thị updatedAt).
    /// Dùng cho header X-Derived-Data-State và cổng Accept check-in, nơi mỗi round-trip tới DB từ xa tốn thời gian.
    /// </summary>
    public static async Task<string> OpenStateAsync(IUnitOfWork uow, Guid userId, AlgorithmOptions opt, CancellationToken ct)
    {
        var open = await uow.Repository<RecalculationJob>().Query()
            .Where(j => j.UserId == userId && j.State != JobState.Succeeded)
            .Select(j => new { j.State, j.AttemptCount })
            .ToListAsync(ct);
        return StateOf(open.Select(j => (j.State, j.AttemptCount)), opt.DerivedJobMaxAttempts);
    }

    /// <summary>
    /// Lỗi DB: <see cref="System.Data.Common.DbException"/>/<see cref="DbUpdateException"/>, hoặc lỗi tạm thời (deadlock 1205, mất kết nối, timeout) mà execution strategy mặc định
    /// của EF SQL Server bọc trong <see cref="InvalidOperationException"/> ("likely due to a transient failure"); chỉ bắt kiểu gốc thì không bao giờ thấy lỗi tạm thời thật.
    /// </summary>
    public static bool IsDbFailure(Exception ex)
        => ex is System.Data.Common.DbException or DbUpdateException || ex is InvalidOperationException { InnerException: System.Data.Common.DbException or DbUpdateException };

    private static string? Trim(string? error) => error is null ? null : error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
}
