using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Web.Admin;

public class TenantsModel : PageModel
{
    private readonly IContext _context;
    private readonly UserManager<BaGetterUser> _userManager;

    public TenantsModel(IContext context, UserManager<BaGetterUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public CreateTenantInput Input { get; set; } = new();

    public IReadOnlyList<TenantInfo> TenantList { get; set; } = [];
    public string StatusMessage { get; set; } = string.Empty;

    public class CreateTenantInput
    {
        [Required, MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(128), RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens.")]
        public string Slug { get; set; } = string.Empty;
    }

    public class TenantInfo
    {
        public Tenant Tenant { get; set; } = null!;
        public int UserCount { get; set; }
        public int PackageCount { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var exists = await _context.Tenants.AnyAsync(t => t.Slug == Input.Slug);
        if (exists)
        {
            ModelState.AddModelError("Input.Slug", "This slug is already taken.");
            await LoadAsync();
            return Page();
        }

        var tenant = new Tenant
        {
            Name = Input.Name,
            Slug = Input.Slug,
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(default);

        StatusMessage = $"Tenant \"{Input.Name}\" created.";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        var tenants = await _context.Tenants
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var list = new List<TenantInfo>();
        foreach (var t in tenants)
        {
            list.Add(new TenantInfo
            {
                Tenant = t,
                UserCount = await _userManager.Users.CountAsync(u => u.TenantId == t.Id),
                PackageCount = await _context.Packages.IgnoreQueryFilters().CountAsync(p => p.TenantId == t.Id),
            });
        }

        TenantList = list;
    }
}
