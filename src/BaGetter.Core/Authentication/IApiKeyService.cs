using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

public interface IApiKeyService
{
    Task<bool> IsValidAsync(string apiKey, CancellationToken cancellationToken);

    Task<ApiKeyValidationResult?> ValidateAsync(string apiKey, CancellationToken cancellationToken);

    Task<(ApiKeyEntity entity, string rawKey)> CreateAsync(
        string name, string userId, string role,
        DateTimeOffset? expiresAt, CancellationToken cancellationToken);

    Task RevokeAsync(int keyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApiKeyEntity>> GetByUserAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApiKeyEntity>> GetAllAsync(CancellationToken cancellationToken);
}
