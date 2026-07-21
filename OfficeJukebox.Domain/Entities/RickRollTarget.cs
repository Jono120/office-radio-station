namespace OfficeJukebox.Domain.Entities;

public sealed class RickRollTarget : Entity
{
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
