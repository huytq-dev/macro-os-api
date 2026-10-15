namespace Application;

public sealed class ChangeProgramStyleCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ChangeProgramStyleCommand, Unit>
{
    public async Task<Unit> Handle(ChangeProgramStyleCommand request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.ProgramType))
            throw new BadRequestException(ExceptionMessages.Invalid("ProgramType"));

        // Đổi style không mất dữ liệu, không reset expenditure (BR-02)
        var program = await _uow.Repository<MacroProgram>().FindTrackedAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException("Chưa có program đang hoạt động.");

        program.ProgramType = request.ProgramType;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
