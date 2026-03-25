using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Web.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<BaGetterUser> _userManager;
    private readonly SignInManager<BaGetterUser> _signInManager;
    private readonly IContext _db;

    public RegisterModel(
        UserManager<BaGetterUser> userManager,
        SignInManager<BaGetterUser> signInManager,
        IContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string Token { get; set; } = string.Empty;

    public class InputModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToPage("/Account/Login");

        var tokenHash = HashToken(token);
        var invitation = await _db.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash && i.AcceptedAt == null);

        if (invitation == null || invitation.ExpiresAt < System.DateTimeOffset.UtcNow)
            return RedirectToPage("/Account/Login");

        Token = token;
        Input.Email = invitation.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var tokenHash = HashToken(Token);
        var invitation = await _db.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash && i.AcceptedAt == null);

        if (invitation == null || invitation.ExpiresAt < System.DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError(string.Empty, "This invitation link is invalid or has expired.");
            return Page();
        }

        var user = new BaGetterUser
        {
            UserName = invitation.Email,
            Email = invitation.Email,
            DisplayName = Input.DisplayName,
            InvitedById = invitation.InvitedById,
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await _userManager.AddToRoleAsync(user, invitation.Role);

        invitation.AcceptedAt = System.DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(default);

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(Url.Content("~/"));
    }

    private static string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hash);
    }
}
