namespace OfficeJukebox.Domain.Entities;

public sealed class SearchTerm : Entity
{
    public string Term { get; set; } = string.Empty;
    public bool IsForbidden { get; set; }
    public string? Category { get; set; }
}
