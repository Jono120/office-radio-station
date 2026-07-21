using OfficeJukebox.Application.Abstractions;

namespace OfficeJukebox.Application;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
}
