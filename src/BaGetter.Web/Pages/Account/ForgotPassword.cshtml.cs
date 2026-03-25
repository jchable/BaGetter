using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<BaGetterUser> _userManager;

    public ForgotPasswordModel(UserManager<BaGetterUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool EmailSent { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Always show success to avoid email enumeration
        EmailSent = true;

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
            return Page();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = Url.Page("/Account/ResetPassword",
            pageHandler: null,
            values: new { token, email = user.Email },
            protocol: Request.Scheme);

        // TODO: Send email with resetUrl via IEmailSender
        // For now, expose in development via log/TempData
        TempData["DevResetUrl"] = resetUrl;

        return Page();
    }
}
