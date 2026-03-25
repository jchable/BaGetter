using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Authentication;

/// <summary>
/// Handles NuGet API key authentication via the X-NuGet-ApiKey header.
/// Used for package push/delete operations from NuGet CLI tools.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeyService)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-NuGet-ApiKey", out var apiKeyValues))
            return AuthenticateResult.NoResult();

        var apiKey = apiKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.NoResult();

        var result = await _apiKeyService.ValidateAsync(apiKey, Context.RequestAborted);
        if (result == null)
        {
            Logger.LogWarning("Invalid API key attempt from {RemoteIP}", Context.Connection?.RemoteIpAddress);
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, result.UserName),
            new Claim(ClaimTypes.NameIdentifier, result.UserId),
            new Claim(ClaimTypes.Role, result.Role),
            new Claim("apikey", "true"),
            new Claim(BaGetter.Authentication.AuthenticationConstants.TenantIdClaim, result.TenantId),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}
