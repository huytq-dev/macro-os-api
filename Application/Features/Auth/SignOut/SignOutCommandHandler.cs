namespace Application;

public sealed class SignOutCommandHandler(IUnitOfWork _uow) : IRequestHandler<SignOutCommand, Unit>
{
    public async Task<Unit> Handle(SignOutCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        _uow.Repository<User>().Update(user);
        await _uow.EnsureSaveAsync(ct);

        return Unit.Value;
    }
}
