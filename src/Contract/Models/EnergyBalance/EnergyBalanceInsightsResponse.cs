namespace Contract;

/// <summary>Insights &amp; Data của hai tab Energy Balance, tính đến hôm nay theo múi giờ user.</summary>
public sealed record EnergyBalanceInsightsResponse(
    DateOnly Today,
    IReadOnlyList<EnergyBalanceRelative> RelativeToExpenditure,
    IReadOnlyList<EnergyBalanceRelative> RelativeToTargets
);
