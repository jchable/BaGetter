using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Web;

[ApiController]
[Route("api/ui/admin")]
[Authorize(Policy = AuthenticationConstants.PolicyCanAdmin)]
public class UiAdminController : ControllerBase
{
    private readonly UserManager<BaGetterUser> _userManager;
    private readonly IContext _db;
    private readonly IInvitationService _invitations;
    private readonly IApiKeyService _apiKeys;
    private readonly IAuditService _audit;

    public UiAdminController(
        UserManager<BaGetterUser> userManager,
        IContext db,
        IInvitationService invitations,
        IApiKeyService apiKeys,
        IAuditService audit)
    {
        _userManager = userManager;
        _db = db;
        _invitations = invitations;
        _apiKeys = apiKeys;
        _audit = audit;
    }

    // --- Dashboard ---

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userCount = _userManager.Users.Count();
        var packageCount = _db.Packages.Count();
        var pendingInvitations = await _db.UserInvitations
            .CountAsync(i => i.AcceptedAt == null && i.ExpiresAt > DateTimeOffset.UtcNow);

        return Ok(new { userCount, packageCount, pendingInvitationCount = pendingInvitations });
    }

    // --- Users ---

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = _userManager.Users.OrderBy(u => u.UserName).ToList();
        var result = new object[users.Count];
        for (var i = 0; i < users.Count; i++)
        {
            var u = users[i];
            var roles = await _userManager.GetRolesAsync(u);
            result[i] = new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.DisplayName,
                role = roles.FirstOrDefault() ?? "None",
                u.TenantId,
                u.CreatedAt,
            };
        }
        return Ok(result);
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> ChangeRole(string userId, [FromBody] ChangeRoleRequest request)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { error = "User not found." });

        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault() ?? "None";

        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!string.IsNullOrEmpty(request.Role) && request.Role != "None")
            await _userManager.AddToRoleAsync(user, request.Role);

        await _audit.LogAsync(AuditAction.RoleChanged,
            currentUser?.Id, currentUser?.UserName,
            "User", userId,
            new { oldRole, newRole = request.Role },
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { success = true });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == userId)
            return BadRequest(new { error = "You cannot delete your own account." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { error = "User not found." });

        await _userManager.DeleteAsync(user);

        await _audit.LogAsync(AuditAction.UserDeleted,
            currentUser?.Id, currentUser?.UserName,
            "User", userId, new { deletedUser = user.UserName },
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { success = true });
    }

    // --- Invitations ---

    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations(CancellationToken ct)
    {
        var invitations = await _invitations.GetAllAsync(ct);
        return Ok(invitations.Select(i => new
        {
            i.Id,
            i.Email,
            i.Role,
            i.InvitedById,
            i.Token,
            i.ExpiresAt,
            i.AcceptedAt,
        }));
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Email))
            return BadRequest(new { error = "Email is required." });

        var currentUser = await _userManager.GetUserAsync(User);
        var invitation = await _invitations.CreateAsync(request.Email, request.Role ?? Roles.Reader, currentUser!.Id, ct);

        await _audit.LogAsync(AuditAction.InvitationCreated,
            currentUser.Id, currentUser.UserName,
            "Invitation", invitation.Id.ToString(),
            new { email = request.Email, role = request.Role },
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var registerUrl = $"{Request.Scheme}://{Request.Host}/account/register?token={Uri.EscapeDataString(invitation.Token)}";

        return Ok(new { id = invitation.Id, registerUrl });
    }

    [HttpDelete("invitations/{invitationId:int}")]
    public async Task<IActionResult> RevokeInvitation(int invitationId, CancellationToken ct)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        await _invitations.RevokeAsync(invitationId, ct);

        await _audit.LogAsync(AuditAction.InvitationRevoked,
            currentUser?.Id, currentUser?.UserName,
            "Invitation", invitationId.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { success = true });
    }

    // --- API Keys (admin) ---

    [HttpGet("api-keys")]
    public async Task<IActionResult> GetAllApiKeys(CancellationToken ct)
    {
        var keys = await _apiKeys.GetAllAsync(ct);
        return Ok(keys.Select(k => new
        {
            k.Id,
            k.Name,
            k.KeyPrefix,
            k.UserId,
            k.Role,
            k.CreatedAt,
            k.ExpiresAt,
            k.LastUsedAt,
            k.IsRevoked,
        }));
    }

    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] AdminCreateApiKeyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest(new { error = "Key name is required." });

        var currentUser = await _userManager.GetUserAsync(User);
        var (entity, rawKey) = await _apiKeys.CreateAsync(
            request.Name, currentUser!.Id, request.Role ?? Roles.Publisher, request.ExpiresAt, ct);

        await _audit.LogAsync(AuditAction.ApiKeyCreated,
            currentUser.Id, currentUser.UserName,
            "ApiKey", entity.Name,
            new { role = request.Role, expiresAt = request.ExpiresAt },
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { key = rawKey, id = entity.Id, name = entity.Name });
    }

    [HttpDelete("api-keys/{keyId:int}")]
    public async Task<IActionResult> RevokeApiKey(int keyId, CancellationToken ct)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        await _apiKeys.RevokeAsync(keyId, ct);

        await _audit.LogAsync(AuditAction.ApiKeyRevoked,
            currentUser?.Id, currentUser?.UserName,
            "ApiKey", keyId.ToString(), null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { success = true });
    }

    // --- Audit Log ---

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] string action = null,
        [FromQuery] string userId = null,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        const int pageSize = 50;
        var skip = (Math.Max(1, page) - 1) * pageSize;

        var logs = await _audit.GetLogsAsync(skip, pageSize, action, userId, ct);
        var total = await _audit.GetCountAsync(action, userId, ct);

        return Ok(new
        {
            logs = logs.Select(l => new
            {
                l.Id,
                l.Timestamp,
                l.UserId,
                l.UserName,
                l.Action,
                l.ResourceType,
                l.ResourceId,
                l.Details,
                l.IpAddress,
            }),
            currentPage = page,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            total,
        });
    }

    // --- Tenants ---

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(CancellationToken ct)
    {
        var tenants = await _db.Tenants.OrderBy(t => t.Name).ToListAsync(ct);
        var result = tenants.Select(t => new
        {
            t.Id,
            t.Name,
            t.Slug,
            t.CreatedAt,
            userCount = _userManager.Users.Count(u => u.TenantId == t.Id),
            packageCount = _db.Packages.Count(p => p.TenantId == t.Id),
        });
        return Ok(result);
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Name) || string.IsNullOrWhiteSpace(request?.Slug))
            return BadRequest(new { error = "Name and slug are required." });

        if (await _db.Tenants.AnyAsync(t => t.Slug == request.Slug, ct))
            return BadRequest(new { error = "A tenant with this slug already exists." });

        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = request.Slug,
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        return Ok(new { id = tenant.Id, name = tenant.Name, slug = tenant.Slug });
    }
}

// --- Request DTOs ---

public class ChangeRoleRequest
{
    public string Role { get; set; }
}

public class CreateInvitationRequest
{
    public string Email { get; set; }
    public string Role { get; set; }
}

public class AdminCreateApiKeyRequest
{
    public string Name { get; set; }
    public string Role { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class CreateTenantRequest
{
    public string Name { get; set; }
    public string Slug { get; set; }
}
