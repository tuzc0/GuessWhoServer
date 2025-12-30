using ClassLibraryGuessWho.Data.Helpers;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace ClassLibraryGuessWho.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const int MAX_MATCH_CODE_GENERATION_ATTEMPTS = 5;

        public long CreateMatchForTournamentQuick(long player1UserId, long player2UserId, DateTime nowUtc)
        {
            if (player1UserId <= 0 || player2UserId <= 0 || player1UserId == player2UserId)
            {
                return INVALID_MATCH_ID;
            }

            for (int attempt = 0; attempt < MAX_MATCH_CODE_GENERATION_ATTEMPTS; attempt++)
            {
                MATCH match = null;
                MATCH_PLAYER host = null;
                MATCH_PLAYER guest = null;

                try
                {
                    match = new MATCH
                    {
                        VISIBILITYID = MatchVisibilityIds.PRIVATE,
                        STATUSID = MatchStatusIds.ACTIVE,
                        MODEID = MatchModeIds.QUICK,
                        MATCHCODE = GenerateMatchCode(),
                        CREATEDATUTC = nowUtc,
                        STARTTIME = nowUtc,
                        ISCODEJOINENABLED = false
                    };

                    host = new MATCH_PLAYER
                    {
                        MATCH = match,
                        USERID = player1UserId,
                        SLOTNUMBER = HOST_SLOT_NUMBER,
                        ISHOST = true,
                        ISREADY = true,
                        JOINEDATUTC = nowUtc
                    };

                    guest = new MATCH_PLAYER
                    {
                        MATCH = match,
                        USERID = player2UserId,
                        SLOTNUMBER = GUEST_SLOT_NUMBER,
                        ISHOST = false,
                        ISREADY = true,
                        JOINEDATUTC = nowUtc
                    };

                    dataContext.MATCH.Add(match);
                    dataContext.MATCH_PLAYER.Add(host);
                    dataContext.MATCH_PLAYER.Add(guest);

                    dataContext.SaveChanges();

                    return match.MATCHID;
                }
                catch (DbUpdateException ex) when (SqlExceptionInspector.IsUniqueConstraintViolation(ex))
                {
                    DetachIfTracked(match);
                    DetachIfTracked(host);
                    DetachIfTracked(guest);
                }
            }

            return INVALID_MATCH_ID;
        }

        private void DetachIfTracked(object entity)
        {
            if (entity == null)
            {
                return;
            }

            DbEntityEntry entry = dataContext.Entry(entity);

            if (entry != null && entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
