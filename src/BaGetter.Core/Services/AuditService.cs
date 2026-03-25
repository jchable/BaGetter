using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Core;

public class AuditService : IAuditService
{
    private readonly IContext _context;

    public AuditService(IContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task LogAsync(
        string action,
        string? userId,
        string? userName,
        string? resourceType = null,
        string? resourceId = null,
        object? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = action,
            UserId = userId,
            UserName = userName,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            IpAddress = ipAddress,
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetLogsAsync(
        int skip = 0,
        int take = 50,
        string? actionFilter = null,
        string? userIdFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(actionFilter, userIdFilter);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        string? actionFilter = null,
        string? userIdFilter = null,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(actionFilter, userIdFilter).CountAsync(cancellationToken);
    }

    private IQueryable<AuditLog> BuildQuery(string? actionFilter, string? userIdFilter)
    {
        IQueryable<AuditLog> query = _context.AuditLogs;

        if (!string.IsNullOrEmpty(actionFilter))
            query = query.Where(a => a.Action == actionFilter);

        if (!string.IsNullOrEmpty(userIdFilter))
            query = query.Where(a => a.UserId == userIdFilter);

        return query;
    }
}
