using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var invitation = new UserInvitation
        {
            Email = email,
            Role = role,
            InvitedById = invitedById,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };

        _context.UserInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

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
        return await _context.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
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
}
