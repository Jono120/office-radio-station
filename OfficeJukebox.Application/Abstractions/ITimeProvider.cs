namespace OfficeJukebox.Application.Abstractions;

public interface ITimeProvider
{
    DateTime Now { get; }
}
