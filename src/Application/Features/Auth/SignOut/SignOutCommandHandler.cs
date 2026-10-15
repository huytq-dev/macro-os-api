namespace Application;

public sealed class SignOutCommandHandler(IUnitOfWork _uow) : IRequestHandler<SignOutCommand, Unit>
{
    public async Task<Unit> Handle(SignOutCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().FindTrackedAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", request.UserId));

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _uow.EnsureSaveAsync(ct);

        return Unit.Value;
    }
}
