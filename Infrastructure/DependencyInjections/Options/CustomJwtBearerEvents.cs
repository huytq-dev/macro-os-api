
namespace Infrastructure;

public class CustomJwtBearerEvents : JwtBearerEvents
{
    public override Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        throw new UnauthorizedException("Access token is missing or invalid.");
    }

    public override Task Forbidden(ForbiddenContext context)
        => throw new ForbiddenException(ExceptionMessages.Forbidden);
}
