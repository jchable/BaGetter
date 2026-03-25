using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string? userId,
        string? userName,
        string? resourceType = null,
        string? resourceId = null,
        object? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetLogsAsync(
        int skip = 0,
        int take = 50,
        string? actionFilter = null,
        string? userIdFilter = null,
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        string? actionFilter = null,
        string? userIdFilter = null,
        CancellationToken cancellationToken = default);
}
