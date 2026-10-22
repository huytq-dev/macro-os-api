namespace Application;

public sealed class ChangeProgramStyleCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<ChangeProgramStyleCommand, Unit>
{
    public async Task<Unit> Handle(ChangeProgramStyleCommand request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.ProgramType))
            throw new BadRequestException(ExceptionMessages.Invalid("ProgramType"));

        // Đổi style không mất dữ liệu, không reset expenditure (BR-02)
        var program = await _uow.Repository<MacroProgram>().Query(tracking: true)
            .Include(p => p.Weeks)
            .Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);

        // Coached do thuật toán tính nên không có target theo ngày do user đặt: bỏ các dòng cũ, target tuần (trung bình) giữ nguyên tới check-in kế
        if (request.ProgramType == ProgramType.Coached && program.DayTargets.Count > 0)
        {
            await _uow.Repository<MacroProgramDayTarget>().RemoveRangeAsync(program.DayTargets.ToList(), ct);
            program.DayTargets.Clear();

            // D2: layout ngày đổi từ hôm nay ⇒ phiên bản tuần mới với snapshot mới (các ngày đã qua giữ snapshot cũ); target tuần giữ nguyên nên không cần kiểm khả thi lại
            var today = (await UserLocale.GetAsync(_uow, _userContext.UserId, _timeProvider, ct)).Today;
            if (program.Weeks.Where(w => w.EffectiveFrom <= today).OrderByDescending(w => w.EffectiveFrom).FirstOrDefault() is { } current)
                ProgramWeekBuilder.UpsertWeek(program, today, new MacroTargets(current.CaloriesTarget, current.ProteinTarget, current.CarbsTarget, current.FatTarget), AdjustmentReason.Manual, validate: false);
        }

        program.ProgramType = request.ProgramType;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
