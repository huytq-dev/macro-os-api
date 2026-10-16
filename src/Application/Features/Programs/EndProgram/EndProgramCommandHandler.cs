namespace Application;

public sealed class EndProgramCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<EndProgramCommand, Unit>
{
    public async Task<Unit> Handle(EndProgramCommand request, CancellationToken ct)
    {
        var program = await _uow.Repository<MacroProgram>().FindTrackedAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException("Chưa có program đang hoạt động.");

        // Giữ lịch sử tuần, chỉ tắt active (UC-B07)
        program.IsActive = false;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
