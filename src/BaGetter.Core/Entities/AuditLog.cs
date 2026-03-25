using System;

namespace BaGetter.Core;

public class AuditLog
{
    public long Id { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    /// <summary>Action performed — use <see cref="AuditAction"/> constants.</summary>
    public string Action { get; set; } = string.Empty;

    public string? ResourceType { get; set; }

    public string? ResourceId { get; set; }

    /// <summary>Additional details serialized as JSON.</summary>
    public string? Details { get; set; }

    public string? IpAddress { get; set; }
}
