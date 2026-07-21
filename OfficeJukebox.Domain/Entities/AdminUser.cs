namespace OfficeJukebox.Domain.Entities;

public sealed class AdminUser : Entity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
