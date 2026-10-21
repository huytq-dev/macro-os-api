namespace Contract;

/// <param name="Changed">false khi <paramref name="From"/> = <paramref name="To"/> ("No Change").</param>
public sealed record GoalEditChangeModel(decimal? From, decimal? To, bool Changed);
