using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

public class ApiKeyService : IApiKeyService
{
    private readonly string _apiKey;
    private readonly ApiKey[] _apiKeys;

    public ApiKeyService(IOptionsSnapshot<BaGetterOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _apiKey = string.IsNullOrEmpty(options.Value.ApiKey) ? null : options.Value.ApiKey;
        _apiKeys = options.Value.Authentication?.ApiKeys ?? [];
    }

    public Task<bool> IsValidAsync(string apiKey, CancellationToken cancellationToken)
    {
        // No API key configured → reject all (no open mode)
        if (_apiKey == null && _apiKeys.Length == 0)
            return Task.FromResult(false);

        return Task.FromResult(
            FixedTimeEquals(_apiKey, apiKey) ||
            _apiKeys.Any(x => FixedTimeEquals(x.Key, apiKey)));
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        if (expected == null || actual == null)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual));
    }
}
