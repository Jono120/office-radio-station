namespace OfficeJukebox.Application.Configuration;

public sealed class QueueRulesOptions
{
    public const string SectionName = "QueueRules";
    public int MaxTracksPerUser { get; set; } = 5;
    public int DontRepeatHours { get; set; } = 4;
}
