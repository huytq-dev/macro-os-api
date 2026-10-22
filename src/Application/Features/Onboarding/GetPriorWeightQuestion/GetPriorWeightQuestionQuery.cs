namespace Application;

/// <summary>Số "X" của câu hỏi Prior Weight. WeightKg null ⇒ dùng cân gần nhất; Unit null ⇒ theo UnitPreference của hồ sơ ("kg" | "lb").</summary>
public sealed record GetPriorWeightQuestionQuery(decimal? WeightKg, string? Unit) : IRequest<PriorWeightQuestionResponse>;
