namespace Contract;

/// <summary>Món/recipe đóng góp vào lượng nutrient của ngày (mỗi entry một dòng).</summary>
public sealed record NutrientContributorModel(string Name, decimal Amount, Guid EntryId);
