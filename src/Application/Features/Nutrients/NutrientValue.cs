namespace Application;

/// <summary>Lượng một chất trên 100 g (hoặc trên 1 khẩu phần với recipe) kèm độ tin cậy.</summary>
public readonly record struct NutrientValue(decimal Amount, NutrientAmountStatus Status);
