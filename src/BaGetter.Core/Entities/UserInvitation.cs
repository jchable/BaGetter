using System;

namespace BaGetter.Core;

public class UserInvitation
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Invited role (Reader, Publisher, Admin, Owner).</summary>
    public string Role { get; set; } = string.Empty;

    public string InvitedById { get; set; } = string.Empty;

    public BaGetterUser? InvitedBy { get; set; }

    /// <summary>Single-use token sent by email.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }
}
