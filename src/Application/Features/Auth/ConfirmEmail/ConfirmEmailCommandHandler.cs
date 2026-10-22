namespace Application;

public sealed class ConfirmEmailCommandHandler(
    IUnitOfWork _uow,
    IRedisCacheService _redis
) : IRequestHandler<ConfirmEmailCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        // Token là bằng chứng sở hữu email nên không cần đăng nhập (đăng nhập bị chặn tới khi xác thực xong)
        var redisKey = $"{VerificationEmailSender.KeyPrefix}{request.Token}";
        var userIdText = await _redis.GetRecordAsync<string>(redisKey)
            ?? throw new BadRequestException(ExceptionMessages.EmailVerificationLinkInvalid);

        if (!Guid.TryParse(userIdText, out var userId))
            throw new BadRequestException(ExceptionMessages.Invalid("Token"));

        var user = await _uow.Repository<User>().FindTrackedAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", userId));

        user.EmailVerified = true;
        await _uow.SaveChangesAsync(ct);

        await _redis.RemoveRecordAsync(redisKey);

        return Unit.Value;
    }
}
