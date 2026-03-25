using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Web.Admin;

public class UsersModel : PageModel
{
    private readonly UserManager<BaGetterUser> _userManager;

    public UsersModel(UserManager<BaGetterUser> userManager)
    {
        _userManager = userManager;
    }

    public IList<UserWithRole> UserList { get; set; } = new List<UserWithRole>();
    public string StatusMessage { get; set; } = string.Empty;

    public class UserWithRole
    {
        public BaGetterUser User { get; set; } = null!;
        public string Role { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            UserList.Add(new UserWithRole
            {
                User = user,
                Role = roles.FirstOrDefault() ?? "None",
            });
        }
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            StatusMessage = "Error: User not found.";
            await OnGetAsync();
            return Page();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        StatusMessage = $"Role updated to {role} for {user.Email}.";
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            StatusMessage = "Error: User not found.";
            await OnGetAsync();
            return Page();
        }

        // Prevent deleting yourself
        var currentUserId = _userManager.GetUserId(User);
        if (userId == currentUserId)
        {
            StatusMessage = "Error: You cannot delete your own account.";
            await OnGetAsync();
            return Page();
        }

        await _userManager.DeleteAsync(user);
        StatusMessage = $"User {user.Email} deleted.";
        await OnGetAsync();
        return Page();
    }
}
