namespace Application;

public sealed class GetCurrentProgramQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetCurrentProgramQuery, ProgramResponse>
{
    public async Task<ProgramResponse> Handle(GetCurrentProgramQuery request, CancellationToken ct)
    {
        var program = await _uow.Repository<MacroProgram>().Query()
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException("Chưa có program đang hoạt động.");

        return program.Adapt<ProgramResponse>();
    }
}
