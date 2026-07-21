namespace OfficeJukebox.Application.Configuration;

public sealed class SkipRulesOptions
{
    public const string SectionName = "SkipRules";
    public int MinimumVetoCount { get; set; } = 3;
}
