namespace Application;

/// <summary>Hoàn tất wizard: cần program đã tạo và đồng ý CẢ HAI điều khoản (Health Disclaimer, Consumer Health Privacy Notice).</summary>
public sealed record CompleteOnboardingCommand(bool AcceptHealthDisclaimer, bool AcceptPrivacyNotice) : IRequest<Unit>;
