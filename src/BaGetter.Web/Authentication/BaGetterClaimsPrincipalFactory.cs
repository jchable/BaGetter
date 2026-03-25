using System.Security.Claims;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Authentication;

/// <summary>
/// Adds the TenantId claim to cookie-authenticated users.
/// </summary>
public class BaGetterClaimsPrincipalFactory : UserClaimsPrincipalFactory<BaGetterUser, BaGetterRole>
{
    public BaGetterClaimsPrincipalFactory(
        UserManager<BaGetterUser> userManager,
        RoleManager<BaGetterRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(BaGetterUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrEmpty(user.TenantId))
        {
            identity.AddClaim(new Claim(AuthenticationConstants.TenantIdClaim, user.TenantId));
        }

        return identity;
    }
}
