using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

/// <summary>
/// Integration tests for the NuGet API key authentication scheme.
/// The service index endpoint (v3/index.json) requires CanRead policy (any authenticated user).
/// Authentication happens via Identity cookie or API key header.
/// </summary>
public class NugetAuthenticationIntegrationTests : IDisposable
{
    private const string ValidApiKey = "test-api-key-1234";

    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;

    public NugetAuthenticationIntegrationTests(ITestOutputHelper output)
    {
        _app = new BaGetterApplication(output, null, dict =>
        {
            dict.Add("ApiKey", ValidApiKey);
        });
        _client = _app.CreateClient();
    }

    [Fact(Skip = "Requires WebApplicationFactory rework for Identity + RateLimiter")]
    public async Task AnonymousAccess_WithNoCredentials_ReturnsUnauthorized()
    {
        using var response = await _client.GetAsync("v3/index.json");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(Skip = "Requires WebApplicationFactory rework for Identity + RateLimiter")]
    public async Task ApiKeyAccess_WithValidKey_ReturnsOk()
    {
        _client.DefaultRequestHeaders.Add("X-NuGet-ApiKey", ValidApiKey);

        using var response = await _client.GetAsync("v3/index.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "Requires WebApplicationFactory rework for Identity + RateLimiter")]
    public async Task ApiKeyAccess_WithInvalidKey_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Add("X-NuGet-ApiKey", "wrong-key");

        using var response = await _client.GetAsync("v3/index.json");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _app.Dispose();
        _client.Dispose();
    }
}
