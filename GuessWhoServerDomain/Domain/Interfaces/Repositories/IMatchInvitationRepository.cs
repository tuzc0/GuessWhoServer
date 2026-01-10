using System;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchInvitationRepository
    {
        bool AddMatchInvitation(long matchId, long inviterUserId, string targetEmail, Guid token, DateTime expiresAtUtc, long targetUserId);
    }
}