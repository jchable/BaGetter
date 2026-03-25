using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter;

internal static class IServiceCollectionExtensions
{
    internal static BaGetterApplication AddBaGetterAuthentication(this BaGetterApplication app, IConfiguration configuration)
    {
        // API key validation service
        app.Services.AddScoped<IApiKeyService, ApiKeyService>();

        // ASP.NET Core Identity — stores are registered by each DB provider via AddBaGetDbContextProvider
        app.Services
            .AddIdentity<BaGetterUser, BaGetterRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.SignIn.RequireConfirmedAccount = false;

                options.Lockout.DefaultLockoutTimeSpan = System.TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<BaGetterClaimsPrincipalFactory>();

        // Cookie scheme (default for browser — redirects to /account/login on 401)
        app.Services.ConfigureApplicationCookie(opts =>
        {
            opts.LoginPath = "/account/login";
            opts.LogoutPath = "/account/logout";
            opts.AccessDeniedPath = "/account/access-denied";
            opts.SlidingExpiration = true;
            opts.Cookie.SameSite = SameSiteMode.Strict;
            opts.Cookie.HttpOnly = true;
            opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

            // API requests should get 401/403, not a redirect to login
            opts.Events.OnRedirectToLogin = context =>
            {
                if (IsApiRequest(context.Request))
                {
                    context.Response.StatusCode = 401;
                    return System.Threading.Tasks.Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return System.Threading.Tasks.Task.CompletedTask;
            };
            opts.Events.OnRedirectToAccessDenied = context =>
            {
                if (IsApiRequest(context.Request))
                {
                    context.Response.StatusCode = 403;
                    return System.Threading.Tasks.Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return System.Threading.Tasks.Task.CompletedTask;
            };
        });

        // Authentication schemes: API key + HTTP Basic + optional OAuth
        var authBuilder = app.Services
            .AddAuthentication()
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                AuthenticationConstants.ApiKeyScheme, _ => { })
            .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
                AuthenticationConstants.BasicScheme, _ => { });

        // OAuth providers — only register when ClientId is configured
        var msClientId = configuration["Authentication:Microsoft:ClientId"];
        if (!string.IsNullOrEmpty(msClientId))
        {
            authBuilder.AddMicrosoftAccount(opts =>
            {
                opts.ClientId = msClientId;
                opts.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"] ?? "";
            });
        }

        var googleClientId = configuration["Authentication:Google:ClientId"];
        if (!string.IsNullOrEmpty(googleClientId))
        {
            authBuilder.AddGoogle(opts =>
            {
                opts.ClientId = googleClientId;
                opts.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? "";
            });
        }

        var githubClientId = configuration["Authentication:GitHub:ClientId"];
        if (!string.IsNullOrEmpty(githubClientId))
        {
            authBuilder.AddGitHub(opts =>
            {
                opts.ClientId = githubClientId;
                opts.ClientSecret = configuration["Authentication:GitHub:ClientSecret"] ?? "";
            });
        }

        // Authorization policies
        app.Services.AddAuthorization(options =>
        {
            // CanRead: any authenticated user (cookie, api key, or basic auth)
            options.AddPolicy(AuthenticationConstants.PolicyCanRead, policy =>
            {
                policy.AddAuthenticationSchemes(
                    AuthenticationConstants.ApiKeyScheme,
                    AuthenticationConstants.BasicScheme,
                    IdentityConstants.ApplicationScheme);
                policy.RequireAuthenticatedUser();
            });

            // CanPublish: api key header only (or roles Publisher / Admin / Owner via cookie)
            options.AddPolicy(AuthenticationConstants.PolicyCanPublish, policy =>
            {
                policy.AddAuthenticationSchemes(
                    AuthenticationConstants.ApiKeyScheme,
                    AuthenticationConstants.BasicScheme,
                    IdentityConstants.ApplicationScheme);
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("apikey", "true") ||
                    ctx.User.IsInRole(Roles.Publisher) ||
                    ctx.User.IsInRole(Roles.Admin) ||
                    ctx.User.IsInRole(Roles.Owner));
            });

            // CanAdmin: Admin or Owner roles via cookie
            options.AddPolicy(AuthenticationConstants.PolicyCanAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Roles.Admin, Roles.Owner);
            });
        });

        return app;
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        if (request.Headers.ContainsKey("X-NuGet-ApiKey"))
            return true;

        if (request.Headers.ContainsKey("Authorization"))
            return true;

        var path = request.Path.Value ?? string.Empty;
        return path.StartsWith("/v3/", System.StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/", System.StringComparison.OrdinalIgnoreCase);
    }
}
