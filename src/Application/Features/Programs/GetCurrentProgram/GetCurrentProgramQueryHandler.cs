namespace Application;

public sealed class GetCurrentProgramQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetCurrentProgramQuery, ProgramResponse>
{
    public async Task<ProgramResponse> Handle(GetCurrentProgramQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var program = await _uow.Repository<MacroProgram>().Query()
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException("Chưa có program đang hoạt động.");

        // Cân nặng mục tiêu: Goal đang hoạt động là nguồn duy nhất (kể cả khi user đã xoá mục tiêu = null); chưa có Goal thì lấy giá trị đồng bộ trên program
        var goal = await _uow.Repository<Goal>().Query()
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .OrderByDescending(g => g.StartDate)
            .Select(g => new { g.TargetWeightKg })
            .FirstOrDefaultAsync(ct);

        // Số ngày tới check-in kế tiếp (cùng quy tắc với preview check-in); program Manual không có check-in
        int? dueInDays = null;
        if (program.ProgramType != ProgramType.Manual)
        {
            var lastCheckIn = await _uow.Repository<WeeklyCheckIn>().Query()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CheckInDate)
                .Select(c => (DateOnly?)c.CheckInDate)
                .FirstOrDefaultAsync(ct);
            var today = FoodLogSupport.Today(_timeProvider);
            var (next, _) = CheckInCalculator.Next(today, program.CheckInWeekday, program.StartDate, lastCheckIn);
            dueInDays = CheckInCalculator.DaysUntil(today, next);
        }

        return program.Adapt<ProgramResponse>() with
        {
            TargetWeightKg = goal is null ? program.TargetWeightKg : goal.TargetWeightKg,
            CheckInDueInDays = dueInDays,
        };
    }
}
