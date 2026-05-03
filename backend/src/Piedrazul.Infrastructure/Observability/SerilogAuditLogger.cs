using Microsoft.Extensions.Logging;
using Piedrazul.Application.Abstractions.Infrastructure;

namespace Piedrazul.Infrastructure.Observability;

public sealed class SerilogAuditLogger(ILogger<SerilogAuditLogger> logger) : IAuditLogger
{
    private readonly ILogger<SerilogAuditLogger> _logger = logger;

    public Task LogAsync(string action, object data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AUDIT {Action} {@Data}", action, data);
        return Task.CompletedTask;
    }
}
