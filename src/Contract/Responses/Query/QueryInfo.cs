namespace Contract;

public sealed class QueryInfo
{
    public int       Top            { get; init; } = AppConstants.DefaultPageTop;
    public int       Skip           { get; init; } = AppConstants.DefaultPageSkip;
    public string?   SearchText     { get; init; }
    public string?   OrderBy        { get; init; } = AppConstants.DefaultOrderBy;
    public OrderType OrderType      { get; init; } = OrderType.Descending;
    public bool      NeedTotalCount { get; init; } = AppConstants.DefaultNeedTotalCount;
}
