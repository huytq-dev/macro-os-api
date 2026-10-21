namespace Application;

/// <summary>Mọi số wizard New Program cần để vẽ và chặn: cân nặng, TDEE hiện tại, sàn kcal chuẩn/thấp, trần kcal/protein, 7 ngày mặc định.</summary>
public sealed record GetProgramSetupQuery : IRequest<ProgramSetupResponse>;
