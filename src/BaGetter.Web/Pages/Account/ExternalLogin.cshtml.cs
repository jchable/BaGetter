using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Account;

public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<BaGetterUser> _signInManager;
    private readonly UserManager<BaGetterUser> _userManager;

    public ExternalLoginModel(
        SignInManager<BaGetterUser> signInManager,
        UserManager<BaGetterUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string ReturnUrl { get; set; } = "/";

    public string ProviderDisplayName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool ShowConfirmForm { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public IActionResult OnGet() => RedirectToPage("/Account/Login");

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return Page();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            return Page();
        }

        // Try to sign in with existing external login
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
            return LocalRedirect(returnUrl);

        if (result.IsLockedOut)
        {
            ErrorMessage = "Account locked. Please try again later.";
            return Page();
        }

        // No existing account — show confirm form with email pre-filled from provider
        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        Input.Email = email;
        ShowConfirmForm = true;
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return Page();
        }

        if (!ModelState.IsValid)
        {
            ShowConfirmForm = true;
            ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Create new user — only if email matches an accepted invitation
            user = new BaGetterUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? Input.Email,
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                ShowConfirmForm = true;
                ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
                return Page();
            }

            // Default role for OAuth users
            await _userManager.AddToRoleAsync(user, Roles.Reader);
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return LocalRedirect(returnUrl);
    }
}
