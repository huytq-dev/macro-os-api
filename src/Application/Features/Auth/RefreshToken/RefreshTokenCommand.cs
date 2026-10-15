namespace Application;

/// <summary>RefreshToken null khi client gửi qua HttpOnly cookie; controller điền vào trước khi Send.</summary>
public sealed record RefreshTokenCommand(
    string ExpiredAccessToken,
    string? RefreshToken
) : IRequest<SignInResponse>;
