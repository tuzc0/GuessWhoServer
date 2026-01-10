using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;

namespace GuessWhoDataAccess.Data.DataAccess.Matches
{
    public sealed partial class MatchInvitationData : IMatchInvitationRepository
    {
        private const byte STATUS_PENDING = 1;
        private const byte STATUS_ACTIVE = 1;
        private const long NO_USER_ID = 0;

        private readonly GuessWhoDBEntities dataContext;

        public MatchInvitationData(GuessWhoDBEntities dataContext)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public bool AddMatchInvitation(long matchId, long inviterUserId, string targetEmail, Guid token, DateTime expiresAtUtc, long targetUserId = NO_USER_ID)
        {
            var entity = new MATCH_INVITATION
            {
                MATCHID = matchId,
                INVITERUSERID = inviterUserId,
                TARGETUSERID = targetUserId > NO_USER_ID ? (long?)targetUserId : null,
                TARGETEMAIL = targetEmail?.Trim().ToLowerInvariant(),
                TOKEN = token,
                INVITATIONSTATUS = STATUS_PENDING,
                STATUSID = STATUS_ACTIVE,
                CREATEDATUTC = DateTime.UtcNow,
                EXPIRESATUTC = expiresAtUtc
            };

            dataContext.MATCH_INVITATION.Add(entity);
            return true;
        }
    }
}