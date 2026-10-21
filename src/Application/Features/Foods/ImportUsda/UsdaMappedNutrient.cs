namespace Application;

/// <summary>Chất đã quy về NutrientDefinition (Id/Code) và lượng /100 g.</summary>
public sealed record UsdaMappedNutrient(Guid DefinitionId, string Code, decimal Amount);
