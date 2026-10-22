namespace Application;

public sealed class EndProgramCommandHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<EndProgramCommand, Unit>
{
    public async Task<Unit> Handle(EndProgramCommand request, CancellationToken ct)
    {
        var program = await _uow.Repository<MacroProgram>().FindTrackedAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);

        // Giữ lịch sử tuần, chỉ tắt active (UC-B07)
        program.IsActive = false;
        program.Status = NutritionProgramStatus.Archived;
        program.EndedAt = (await _clock.GetAsync(ct)).Today; // ngày kết thúc tính vào program (target còn đọc được tới hết ngày này)
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
