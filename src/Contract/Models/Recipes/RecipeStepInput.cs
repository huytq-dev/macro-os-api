namespace Contract;

/// <summary>Một bước làm khi tạo/sửa recipe: Order thiếu thì theo vị trí trong mảng; Text ≤ 300 ký tự.</summary>
public sealed record RecipeStepInput(int? Order, string Text);
