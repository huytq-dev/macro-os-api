namespace Application;

public sealed class ConfirmEmailCommandHandler(
    IUnitOfWork _uow,
    IRedisCacheService _redis
) : IRequestHandler<ConfirmEmailCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var redisKey = $"email-verify:{request.UserId}";
        var storedToken = await _redis.GetRecordAsync<string>(redisKey)
            ?? throw new BadRequestException(ExceptionMessages.Invalid("Token"));

        if (storedToken != request.Token)
            throw new BadRequestException(ExceptionMessages.Invalid("Token"));

        var user = await _uow.Repository<User>().FindTrackedAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", request.UserId));

        user.EmailVerified = true;
        await _uow.SaveChangesAsync(ct);

        await _redis.RemoveRecordAsync(redisKey);

        return Unit.Value;
    }
}
