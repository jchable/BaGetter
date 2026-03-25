using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Admin;

public class ApiKeysModel : PageModel
{
    private readonly IApiKeyService _apiKeyService;
    private readonly UserManager<BaGetterUser> _userManager;
    private readonly IAuditService _audit;

    public ApiKeysModel(IApiKeyService apiKeyService, UserManager<BaGetterUser> userManager, IAuditService audit)
    {
        _apiKeyService = apiKeyService;
        _userManager = userManager;
        _audit = audit;
    }

    [BindProperty]
    public CreateKeyInput Input { get; set; } = new();

    public IReadOnlyList<ApiKeyEntity> KeyList { get; set; } = [];
    public string StatusMessage { get; set; } = string.Empty;
    public string? NewlyCreatedKey { get; set; }

    public class CreateKeyInput
    {
        [Required, MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = Roles.Publisher;

        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public async Task OnGetAsync()
    {
        KeyList = await _apiKeyService.GetAllAsync(default);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            KeyList = await _apiKeyService.GetAllAsync(default);
            return Page();
        }

        var userId = _userManager.GetUserId(User)!;
        var (entity, rawKey) = await _apiKeyService.CreateAsync(
            Input.Name, userId, Input.Role, Input.ExpiresAt, default);

        NewlyCreatedKey = rawKey;

        await _audit.LogAsync(AuditAction.ApiKeyCreated,
            userId, User.Identity?.Name,
            "ApiKey", entity.Name,
            new { role = Input.Role, expiresAt = Input.ExpiresAt?.ToString("o") },
            HttpContext.Connection.RemoteIpAddress?.ToString());

        StatusMessage = $"API key \"{Input.Name}\" created. Copy it now — it won't be shown again.";
        KeyList = await _apiKeyService.GetAllAsync(default);
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int keyId)
    {
        await _apiKeyService.RevokeAsync(keyId, default);

        await _audit.LogAsync(AuditAction.ApiKeyRevoked,
            _userManager.GetUserId(User), User.Identity?.Name,
            "ApiKey", keyId.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        StatusMessage = "API key revoked.";
        KeyList = await _apiKeyService.GetAllAsync(default);
        return Page();
    }
}
