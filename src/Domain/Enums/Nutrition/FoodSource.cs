namespace Domain;

// QuickAdd: FoodItem ẩn tạo khi user nhập thẳng kcal/macro, giữ constraint XOR của FoodLogEntry
public enum FoodSource { System, UserCreated, OpenFoodFacts, USDA, QuickAdd }
