namespace Application;

public sealed record RefreshTokenCommand(
    string ExpiredAccessToken,
    string RefreshToken
) : IRequest<SignInResponse>;
