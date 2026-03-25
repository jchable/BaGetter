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

[Microsoft.AspNetCore.Authorization.Authorize]
public class ManageModel : PageModel
{
    private readonly UserManager<BaGetterUser> _userManager;
    private readonly SignInManager<BaGetterUser> _signInManager;
    private readonly IApiKeyService _apiKeyService;
    private readonly IAuditService _audit;

    public ManageModel(
        UserManager<BaGetterUser> userManager,
        SignInManager<BaGetterUser> signInManager,
        IApiKeyService apiKeyService,
        IAuditService audit)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _apiKeyService = apiKeyService;
        _audit = audit;
    }

    [BindProperty]
    public ProfileInputModel Input { get; set; } = new();

    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = new();

    [BindProperty]
    public CreateApiKeyInput ApiKeyInput { get; set; } = new();

    public string Email { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public bool HasPassword { get; set; }
    public IList<UserLoginInfo> ExternalLogins { get; set; } = new List<UserLoginInfo>();
    public IReadOnlyList<ApiKeyEntity> UserApiKeys { get; set; } = [];
    public string? NewlyCreatedKey { get; set; }

    public class ProfileInputModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; } = string.Empty;
    }

    public class PasswordInputModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class CreateApiKeyInput
    {
        [Required, MaxLength(256)]
        [Display(Name = "Key name")]
        public string Name { get; set; } = string.Empty;
    }

    private async Task LoadAsync(BaGetterUser user)
    {
        Email = user.Email ?? string.Empty;
        Input.DisplayName = user.DisplayName;
        HasPassword = await _userManager.HasPasswordAsync(user);
        ExternalLogins = await _userManager.GetLoginsAsync(user);
        UserApiKeys = await _apiKeyService.GetByUserAsync(user.Id, default);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        user.DisplayName = Input.DisplayName;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Profile updated.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, PasswordInput.CurrentPassword, PasswordInput.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await LoadAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Password changed.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostSetPasswordAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var result = await _userManager.AddPasswordAsync(user, PasswordInput.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await LoadAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Password set.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        if (!result.Succeeded)
        {
            StatusMessage = "Error: Could not remove login.";
            await LoadAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Login removed.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateApiKeyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var (entity, rawKey) = await _apiKeyService.CreateAsync(
            ApiKeyInput.Name, user.Id, Roles.Publisher, null, default);

        NewlyCreatedKey = rawKey;

        await _audit.LogAsync(AuditAction.ApiKeyCreated,
            user.Id, user.UserName,
            "ApiKey", entity.Name, null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        StatusMessage = $"API key \"{ApiKeyInput.Name}\" created. Copy it now — it won't be shown again.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeApiKeyAsync(int keyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // Verify the key belongs to the current user
        var keys = await _apiKeyService.GetByUserAsync(user.Id, default);
        if (!keys.Any(k => k.Id == keyId))
        {
            StatusMessage = "Error: Key not found.";
            await LoadAsync(user);
            return Page();
        }

        await _apiKeyService.RevokeAsync(keyId, default);

        await _audit.LogAsync(AuditAction.ApiKeyRevoked,
            user.Id, user.UserName,
            "ApiKey", keyId.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        StatusMessage = "API key revoked.";
        await LoadAsync(user);
        return Page();
    }
}
