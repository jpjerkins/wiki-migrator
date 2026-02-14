using MediatR;
using Microsoft.Extensions.Logging;

namespace WikiMigrator.Application.Queries;

public class GetMigrationStatusHandler : IRequestHandler<GetMigrationStatusQuery, MigrationStatus>
{
    private readonly ILogger<GetMigrationStatusHandler> _logger;
    
    // Simple in-memory store for migration statuses
    private static readonly Dictionary<string, MigrationStatus> _migrationStatuses = new();

    public GetMigrationStatusHandler(ILogger<GetMigrationStatusHandler> logger)
    {
        _logger = logger;
    }

    public Task<MigrationStatus> Handle(GetMigrationStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting migration status for: {MigrationId}", request.MigrationId);

        if (_migrationStatuses.TryGetValue(request.MigrationId, out var status))
        {
            return Task.FromResult(status);
        }

        _logger.LogWarning("Migration status not found for: {MigrationId}", request.MigrationId);
        
        return Task.FromResult(new MigrationStatus
        {
            MigrationId = request.MigrationId,
            Status = "NotFound"
        });
    }

    public static void UpdateStatus(MigrationStatus status)
    {
        _migrationStatuses[status.MigrationId] = status;
    }

    public static void ClearStatuses()
    {
        _migrationStatuses.Clear();
    }
}
