using System;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace BaGetter.Web.Authentication;

/// <summary>
/// Handles HTTP Basic authentication for NuGet protocol clients.
/// The password is validated as an API key via <see cref="IApiKeyService"/>.
/// The username is ignored — only the password matters.
/// </summary>
public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
{
    private readonly IApiKeyService _apiKeyService;

    public BasicAuthenticationHandler(
        IOptionsMonitor<BasicAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeyService)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        string password;
        try
        {
            var encoded = headerValue["Basic ".Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var colonIndex = decoded.IndexOf(':');
            password = colonIndex >= 0 ? decoded[(colonIndex + 1)..] : decoded;
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Invalid Basic authentication header encoding.");
        }

        if (string.IsNullOrWhiteSpace(password))
            return AuthenticateResult.Fail("Empty credentials.");

        var valid = await _apiKeyService.IsValidAsync(password, Context.RequestAborted);
        if (!valid)
            return AuthenticateResult.Fail("Invalid credentials.");

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "basic-apikey"),
            new Claim(ClaimTypes.Role, Roles.Reader),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.WWWAuthenticate = "Basic realm=\"BaGetter\", charset=\"UTF-8\"";
        return Task.CompletedTask;
    }
}
