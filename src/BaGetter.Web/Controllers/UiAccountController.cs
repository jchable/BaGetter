using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Web;

[ApiController]
[Route("api/ui/account")]
public class UiAccountController : ControllerBase
{
    private readonly UserManager<BaGetterUser> _userManager;
    private readonly SignInManager<BaGetterUser> _signInManager;
    private readonly IApiKeyService _apiKeyService;
    private readonly IAuditService _audit;
    private readonly IContext _db;

    public UiAccountController(
        UserManager<BaGetterUser> userManager,
        SignInManager<BaGetterUser> signInManager,
        IApiKeyService apiKeyService,
        IAuditService audit,
        IContext db)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(apiKeyService);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(db);
        _userManager = userManager;
        _signInManager = signInManager;
        _apiKeyService = apiKeyService;
        _audit = audit;
        _db = db;
    }

    // --- Auth ---

    [HttpGet("me")]
    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var hasPassword = await _userManager.HasPasswordAsync(user);
        var logins = await _userManager.GetLoginsAsync(user);

        return Ok(new
        {
            id = user.Id,
            userName = user.UserName,
            email = user.Email,
            displayName = user.DisplayName,
            roles = roles.ToArray(),
            tenantId = string.IsNullOrEmpty(user.TenantId) ? null : user.TenantId,
            hasPassword,
            externalLogins = logins.Select(l => new { l.LoginProvider, l.ProviderDisplayName }).ToArray(),
        });
    }

    [HttpGet("oauth-providers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOAuthProviders()
    {
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        var providers = schemes
            .Select(s => new { name = s.Name, displayName = s.DisplayName ?? s.Name })
            .ToList();
        return Ok(providers);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
            return BadRequest(new { error = "Email and password are required." });

        var result = await _signInManager.PasswordSignInAsync(
            request.Email, request.Password, request.RememberMe, lockoutOnFailure: true);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (result.Succeeded)
        {
            var user = await _signInManager.UserManager.FindByEmailAsync(request.Email);
            await _audit.LogAsync(AuditAction.LoginSuccess, user?.Id, request.Email, ipAddress: ip);
            return Ok(new { success = true });
        }

        if (result.IsLockedOut)
        {
            await _audit.LogAsync(AuditAction.LoginFailure, null, request.Email,
                details: new { reason = "LockedOut" }, ipAddress: ip);
            return BadRequest(new { error = "Account locked. Please try again later." });
        }

        await _audit.LogAsync(AuditAction.LoginFailure, null, request.Email,
            details: new { reason = "InvalidCredentials" }, ipAddress: ip);
        return BadRequest(new { error = "Invalid email or password." });
    }

    [HttpPost("logout")]
    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { success = true });
    }

    // --- OAuth ---

    [HttpGet("external-login")]
    [AllowAnonymous]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string returnUrl = "/")
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("external-login/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string returnUrl = "/", [FromQuery] string remoteError = null)
    {
        if (remoteError != null)
            return Redirect($"/account/login?error={Uri.EscapeDataString(remoteError)}");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return Redirect("/account/login?error=external_login_failed");

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
            return Redirect(returnUrl);

        if (result.IsLockedOut)
            return Redirect("/account/login?error=locked_out");

        // No existing account — auto-create with Reader role
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            return Redirect("/account/login?error=no_email_from_provider");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new BaGetterUser
            {
                UserName = email,
                Email = email,
                DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
                TenantId = "default",
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return Redirect($"/account/login?error={Uri.EscapeDataString("Failed to create account.")}");

            await _userManager.AddToRoleAsync(user, Roles.Reader);
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return Redirect(returnUrl);
    }

    // --- Registration ---

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
            return BadRequest(new { error = "Invitation token is required." });

        var tokenHash = HashToken(request.Token);
        var invitation = await _db.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash && i.AcceptedAt == null);

        if (invitation == null || invitation.ExpiresAt < DateTimeOffset.UtcNow)
            return BadRequest(new { error = "This invitation link is invalid or has expired." });

        var inviter = await _userManager.FindByIdAsync(invitation.InvitedById);
        var tenantId = inviter?.TenantId ?? "default";

        var user = new BaGetterUser
        {
            UserName = invitation.Email,
            Email = invitation.Email,
            DisplayName = request.DisplayName ?? invitation.Email,
            InvitedById = invitation.InvitedById,
            TenantId = tenantId,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        await _userManager.AddToRoleAsync(user, invitation.Role);
        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(default);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return Ok(new { success = true });
    }

    // --- Profile ---

    [HttpPut("profile")]
    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        user.DisplayName = request.DisplayName ?? user.DisplayName;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        return Ok(new { success = true });
    }

    [HttpPut("password")]
    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var hasPassword = await _userManager.HasPasswordAsync(user);

        IdentityResult result;
        if (hasPassword)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequest(new { error = "Current password is required." });
            result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        else
        {
            result = await _userManager.AddPasswordAsync(user, request.NewPassword);
        }

        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        await _signInManager.RefreshSignInAsync(user);
        return Ok(new { success = true });
    }

    // --- API Keys (user's own) ---

    [HttpGet("api-keys")]
    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> GetApiKeys(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var keys = await _apiKeyService.GetByUserAsync(user.Id, cancellationToken);
        return Ok(keys.Select(k => new
        {
            k.Id,
            k.Name,
            keyPrefix = k.KeyPrefix,
            k.CreatedAt,
            k.ExpiresAt,
            k.LastUsedAt,
            k.IsRevoked,
        }));
    }

    [HttpPost("api-keys")]
    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest(new { error = "Key name is required." });

        var (entity, rawKey) = await _apiKeyService.CreateAsync(
            request.Name, user.Id, Roles.Publisher, null, cancellationToken);

        await _audit.LogAsync(AuditAction.ApiKeyCreated,
            user.Id, user.UserName,
            "ApiKey", entity.Name, null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { key = rawKey, id = entity.Id, name = entity.Name });
    }

    [HttpDelete("api-keys/{keyId:int}")]
    [Authorize(Policy = AuthenticationConstants.PolicyCanRead)]
    public async Task<IActionResult> RevokeApiKey(int keyId, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var keys = await _apiKeyService.GetByUserAsync(user.Id, cancellationToken);
        if (!keys.Any(k => k.Id == keyId))
            return NotFound(new { error = "Key not found." });

        await _apiKeyService.RevokeAsync(keyId, cancellationToken);

        await _audit.LogAsync(AuditAction.ApiKeyRevoked,
            user.Id, user.UserName,
            "ApiKey", keyId.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { success = true });
    }

    // --- DTOs ---

    private static string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hash);
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}

public class RegisterRequest
{
    public string Token { get; set; }
    public string DisplayName { get; set; }
    public string Password { get; set; }
}

public class UpdateProfileRequest
{
    public string DisplayName { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}

public class CreateApiKeyRequest
{
    public string Name { get; set; }
}
