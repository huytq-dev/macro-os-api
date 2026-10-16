namespace Application;

public sealed class ChangePasswordCommandHandler(
    IUnitOfWork _uow,
    IPasswordHasher _hasher,
    IUserContext _userContext
) : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().FindTrackedAsync(u => u.Id == _userContext.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", _userContext.UserId));

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException(ExceptionMessages.Invalid("Current password"));

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _uow.EnsureSaveAsync(ct);

        return Unit.Value;
    }
}
