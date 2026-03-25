using System;

namespace BaGetter.Core;

public class ApiKeyEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>SHA256 hash of the raw API key.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>First 8 characters of the raw key, for display only.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public bool IsRevoked { get; set; }

    public string UserId { get; set; } = string.Empty;

    public BaGetterUser? User { get; set; }

    /// <summary>Role granted when this key is used (Publisher or Reader).</summary>
    public string Role { get; set; } = Roles.Publisher;
}
