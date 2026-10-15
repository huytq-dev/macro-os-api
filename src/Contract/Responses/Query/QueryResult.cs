namespace Contract;

public sealed class QueryResult<T>
{
    public IReadOnlyList<T> Items      { get; init; } = [];
    public int?             TotalCount { get; init; }
}
