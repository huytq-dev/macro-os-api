namespace Contract;

public sealed class EmailOptions
{
    public string SenderEmail { get; init; } = "";
    public string SenderName  { get; init; } = "MacroOS";
    public string SmtpHost    { get; init; } = "";
    public int    SmtpPort    { get; init; } = 587;
    public string AppPassword { get; init; } = "";
}
