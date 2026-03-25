using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<BaGetterUser> _signInManager;
    private readonly IAuditService _audit;

    public LoginModel(SignInManager<BaGetterUser> signInManager, IAuditService audit)
    {
        _signInManager = signInManager;
        _audit = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; set; } = "/";

    public IReadOnlyList<OAuthProviderInfo> OAuthProviders { get; set; } = [];

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public record OAuthProviderInfo(string Name, string DisplayName);

    public async Task OnGetAsync(string returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        OAuthProviders = await GetExternalProvidersAsync();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            OAuthProviders = await GetExternalProvidersAsync();
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (result.Succeeded)
        {
            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            await _audit.LogAsync(AuditAction.LoginSuccess, user?.Id, Input.Email,
                ipAddress: ip);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            await _audit.LogAsync(AuditAction.LoginFailure, null, Input.Email,
                details: new { reason = "LockedOut" }, ipAddress: ip);
            ModelState.AddModelError(string.Empty, "Account locked. Please try again later.");
            OAuthProviders = await GetExternalProvidersAsync();
            return Page();
        }

        await _audit.LogAsync(AuditAction.LoginFailure, null, Input.Email,
            details: new { reason = "InvalidCredentials" }, ipAddress: ip);
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        OAuthProviders = await GetExternalProvidersAsync();
        return Page();
    }

    public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        var redirectUrl = Url.Page("/Account/ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    private async Task<IReadOnlyList<OAuthProviderInfo>> GetExternalProvidersAsync()
    {
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        return schemes
            .Select(s => new OAuthProviderInfo(s.Name, s.DisplayName ?? s.Name))
            .ToList();
    }
}
