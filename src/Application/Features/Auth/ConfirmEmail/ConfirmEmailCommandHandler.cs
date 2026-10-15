namespace Application;

public sealed class ConfirmEmailCommandHandler(
    IUnitOfWork _uow,
    IRedisCacheService _redis,
    IUserContext _userContext
) : IRequestHandler<ConfirmEmailCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var redisKey = $"email-verify:{userId}";
        var storedToken = await _redis.GetRecordAsync<string>(redisKey)
            ?? throw new BadRequestException(ExceptionMessages.Invalid("Token"));

        if (storedToken != request.Token)
            throw new BadRequestException(ExceptionMessages.Invalid("Token"));

        var user = await _uow.Repository<User>().FindTrackedAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", userId));

        user.EmailVerified = true;
        await _uow.SaveChangesAsync(ct);

        await _redis.RemoveRecordAsync(redisKey);

        return Unit.Value;
    }
}
