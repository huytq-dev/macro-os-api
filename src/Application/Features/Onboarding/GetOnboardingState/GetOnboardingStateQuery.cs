namespace Application;

/// <summary>Trạng thái wizard thiết lập: bước đang dở, đáp án đã lưu, đã có program/đồng ý điều khoản chưa.</summary>
public sealed record GetOnboardingStateQuery : IRequest<OnboardingStateResponse>;
