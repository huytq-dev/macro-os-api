namespace Contract;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

public sealed record ConfirmEmailRequest(string Token);
