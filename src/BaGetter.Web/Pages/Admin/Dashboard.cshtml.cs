using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Admin;

public class DashboardModel : PageModel
{
    private readonly UserManager<BaGetterUser> _userManager;
    private readonly IContext _context;

    public DashboardModel(UserManager<BaGetterUser> userManager, IContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public int UserCount { get; set; }
    public int PackageCount { get; set; }
    public int PendingInvitationCount { get; set; }

    public async Task OnGetAsync()
    {
        UserCount = _userManager.Users.Count();
        PackageCount = _context.Packages.Count();
        PendingInvitationCount = _context.UserInvitations
            .Count(i => i.AcceptedAt == null && i.ExpiresAt > System.DateTimeOffset.UtcNow);
    }
}
