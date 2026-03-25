using Microsoft.AspNetCore.Identity;

namespace BaGetter.Core;

public class BaGetterRole : IdentityRole
{
    public BaGetterRole() { }

    public BaGetterRole(string roleName) : base(roleName) { }
}
