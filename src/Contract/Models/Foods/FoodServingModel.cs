namespace Contract;

/// <param name="Label">Tên hiển thị cho chip đơn vị: ServingName bỏ số "1 " đầu ("1 large - 3 1/4\" diameter" → "large - 3 1/4\" diameter").</param>
public sealed record FoodServingModel(Guid Id, string ServingName, decimal WeightInGrams, bool IsDefault, string Label = "");
