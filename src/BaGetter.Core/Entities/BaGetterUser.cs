using System;
using Microsoft.AspNetCore.Identity;

namespace BaGetter.Core;

public class BaGetterUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Id of the user who invited this user. Null for the first admin (self-created).</summary>
    public string? InvitedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
