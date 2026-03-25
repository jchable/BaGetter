using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Core;

public class InvitationService : IInvitationService
{
    private readonly IContext _context;

    public InvitationService(IContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserInvitation> CreateAsync(string email, string role, string invitedById, CancellationToken cancellationToken = default)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(rawToken);

        var invitation = new UserInvitation
        {
            Email = email,
            Role = role,
            InvitedById = invitedById,
            Token = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(2),
        };

        _context.UserInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        // Return the raw token so the caller can send it via email.
        // The database only stores the hash.
        invitation.Token = rawToken;
        return invitation;
    }

    public async Task<IReadOnlyList<UserInvitation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserInvitations
            .Include(i => i.InvitedBy)
            .OrderByDescending(i => i.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserInvitation?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        return await _context.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == tokenHash, cancellationToken);
    }

    public async Task RevokeAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await _context.UserInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        if (invitation != null)
        {
            _context.UserInvitations.Remove(invitation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Hash a raw invitation token with SHA-256 so the database never stores the original value.
    /// </summary>
    private static string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hash);
    }
}
