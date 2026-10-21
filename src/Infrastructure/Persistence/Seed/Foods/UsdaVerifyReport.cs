namespace Infrastructure;

public sealed record UsdaVerifyReport(int Total, int Requests, IReadOnlyList<UsdaVerifyItem> Items, bool Apply)
{
    public int Count(string status) => Items.Count(i => i.Status == status);
}
