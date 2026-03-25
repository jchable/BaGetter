using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Admin;

public class InvitationsModel : PageModel
{
    private readonly IInvitationService _invitationService;
    private readonly UserManager<BaGetterUser> _userManager;

    public InvitationsModel(IInvitationService invitationService, UserManager<BaGetterUser> userManager)
    {
        _invitationService = invitationService;
        _userManager = userManager;
    }

    [BindProperty]
    public InviteInputModel Input { get; set; } = new();

    public IReadOnlyList<UserInvitation> InvitationList { get; set; } = [];

    public string StatusMessage { get; set; } = string.Empty;
    public string? LastInviteUrl { get; set; }

    public class InviteInputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = Roles.Reader;
    }

    public async Task OnGetAsync()
    {
        InvitationList = await _invitationService.GetAllAsync();
    }

    public async Task<IActionResult> OnPostInviteAsync()
    {
        if (!ModelState.IsValid)
        {
            InvitationList = await _invitationService.GetAllAsync();
            return Page();
        }

        var currentUserId = _userManager.GetUserId(User);
        var invitation = await _invitationService.CreateAsync(
            Input.Email, Input.Role, currentUserId!, default);

        var registerUrl = Url.Page("/Account/Register",
            pageHandler: null,
            values: new { token = invitation.Token },
            protocol: Request.Scheme);

        LastInviteUrl = registerUrl;
        StatusMessage = $"Invitation created for {Input.Email}.";
        InvitationList = await _invitationService.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int invitationId)
    {
        await _invitationService.RevokeAsync(invitationId);
        StatusMessage = "Invitation revoked.";
        InvitationList = await _invitationService.GetAllAsync();
        return Page();
    }
}
