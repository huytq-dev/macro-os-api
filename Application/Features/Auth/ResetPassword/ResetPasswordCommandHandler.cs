namespace Application;

public sealed class ResetPasswordCommandHandler(
    IUnitOfWork _uow,
    IPasswordHasher _hasher,
    IRedisCacheService _redis
) : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var redisKey = $"reset-password:{request.Token}";
        var userIdStr = await _redis.GetRecordAsync<string>(redisKey)
            ?? throw new BadRequestException(ExceptionMessages.Invalid("Token"));

        if (!Guid.TryParse(userIdStr, out var userId))
            throw new BadRequestException(ExceptionMessages.Invalid("Token"));

        var user = await _uow.Repository<User>().GetByIdAsync(userId);

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        _uow.Repository<User>().Update(user);
        await _uow.EnsureSaveAsync(ct);

        await _redis.RemoveRecordAsync(redisKey);

        return Unit.Value;
    }
}
