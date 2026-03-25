using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

public interface IInvitationService
{
    Task<UserInvitation> CreateAsync(string email, string role, string invitedById, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserInvitation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserInvitation?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAsync(int invitationId, CancellationToken cancellationToken = default);
}
