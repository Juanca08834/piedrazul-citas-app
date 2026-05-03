namespace Piedrazul.Application.Abstractions.Infrastructure;

public interface IAuditLogger
{
    Task LogAsync(string action, object data, CancellationToken cancellationToken = default);
}
