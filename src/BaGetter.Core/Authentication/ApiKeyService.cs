using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

public class ApiKeyService : IApiKeyService
{
    private readonly IContext _context;
    private readonly string? _configApiKey;
    private readonly ApiKey[] _configApiKeys;

    public ApiKeyService(IContext context, IOptionsSnapshot<BaGetterOptions> options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _context = context;
        _configApiKey = string.IsNullOrEmpty(options.Value.ApiKey) ? null : options.Value.ApiKey;
        _configApiKeys = options.Value.Authentication?.ApiKeys ?? [];
    }

    public async Task<bool> IsValidAsync(string apiKey, CancellationToken cancellationToken)
    {
        return await ValidateAsync(apiKey, cancellationToken) != null;
    }

    public async Task<ApiKeyValidationResult?> ValidateAsync(string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        // 1. Check database keys first
        var hash = ComputeHash(apiKey);
        var now = DateTimeOffset.UtcNow;

        var entity = await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k =>
                k.KeyHash == hash &&
                !k.IsRevoked &&
                (k.ExpiresAt == null || k.ExpiresAt > now),
                cancellationToken);

        if (entity != null)
        {
            // Update LastUsedAt (fire-and-forget style, don't block auth)
            entity.LastUsedAt = now;
            try { await _context.SaveChangesAsync(cancellationToken); } catch { /* non-critical */ }

            return new ApiKeyValidationResult
            {
                KeyId = entity.Id,
                UserId = entity.UserId,
                UserName = entity.User?.UserName ?? entity.UserId,
                Role = entity.Role,
                TenantId = entity.User?.TenantId ?? string.Empty,
            };
        }

        // 2. Fallback to config-based keys
        if (FixedTimeEquals(_configApiKey, apiKey) ||
            _configApiKeys.Any(x => FixedTimeEquals(x.Key, apiKey)))
        {
            return new ApiKeyValidationResult
            {
                KeyId = 0,
                UserId = string.Empty,
                UserName = "apikey",
                Role = Roles.Publisher,
            };
        }

        return null;
    }

    public async Task<(ApiKeyEntity entity, string rawKey)> CreateAsync(
        string name, string userId, string role,
        DateTimeOffset? expiresAt, CancellationToken cancellationToken)
    {
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = Convert.ToBase64String(rawBytes);
        var hash = ComputeHash(rawKey);
        var prefix = rawKey[..8];

        var entity = new ApiKeyEntity
        {
            Name = name,
            KeyHash = hash,
            KeyPrefix = prefix,
            UserId = userId,
            Role = role,
            ExpiresAt = expiresAt,
        };

        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return (entity, rawKey);
    }

    public async Task RevokeAsync(int keyId, CancellationToken cancellationToken)
    {
        var entity = await _context.ApiKeys.FindAsync([keyId], cancellationToken);
        if (entity != null)
        {
            entity.IsRevoked = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ApiKeyEntity>> GetByUserAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKeyEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.ApiKeys
            .Include(k => k.User)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static bool FixedTimeEquals(string? expected, string? actual)
    {
        if (expected == null || actual == null)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual));
    }
}
