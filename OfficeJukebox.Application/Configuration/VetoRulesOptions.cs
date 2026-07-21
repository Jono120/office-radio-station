namespace OfficeJukebox.Application.Configuration;

public sealed class VetoRulesOptions
{
    public const string SectionName = "VetoRules";
    public int DailyLimit { get; set; } = 20;
}
