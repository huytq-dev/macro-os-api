namespace Application;

public sealed class ChangeProgramStyleCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ChangeProgramStyleCommand, Unit>
{
    public async Task<Unit> Handle(ChangeProgramStyleCommand request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.ProgramType))
            throw new BadRequestException(ExceptionMessages.Invalid("ProgramType"));

        // Đổi style không mất dữ liệu, không reset expenditure (BR-02)
        var program = await _uow.Repository<MacroProgram>().Query(tracking: true)
            .Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);

        // Coached do thuật toán tính nên không có target theo ngày do user đặt: bỏ các dòng cũ, target tuần (trung bình) giữ nguyên tới check-in kế
        if (request.ProgramType == ProgramType.Coached && program.DayTargets.Count > 0)
            await _uow.Repository<MacroProgramDayTarget>().RemoveRangeAsync(program.DayTargets.ToList(), ct);

        program.ProgramType = request.ProgramType;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
