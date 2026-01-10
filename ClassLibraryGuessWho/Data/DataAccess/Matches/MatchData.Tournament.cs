using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const int MAX_MATCH_CODE_GENERATION_ATTEMPTS = 5;

        public TournamentMatchCreationResult CreateMatchForTournamentQuick(CreateTournamentMatchArgs tournamentArgs)
        {
            if (tournamentArgs == null)
            {
                return default;
            }

            if (tournamentArgs.Player1UserId <= 0 || tournamentArgs.Player2UserId <= 0 || 
                tournamentArgs.Player1UserId == tournamentArgs.Player2UserId)
            {
                return default;
            }

            for (int attempt = 0; attempt < MAX_MATCH_CODE_GENERATION_ATTEMPTS; attempt++)
            {
                string matchCode = GenerateMatchCode();

                if (MatchCodeExists(matchCode))
                {
                    continue;
                }

                var match = new MATCH
                {
                    VISIBILITYID = MatchVisibilityIds.PRIVATE,
                    STATUSID = MatchStatusIds.ACTIVE,
                    MODEID = MatchModeIds.QUICK,
                    MATCHCODE = GenerateMatchCode(),
                    CREATEDATUTC = tournamentArgs.NowUtc,
                    STARTTIME = tournamentArgs.NowUtc,
                    ISCODEJOINENABLED = false
                };

                var host = new MATCH_PLAYER
                {
                    MATCH = match,
                    USERID = tournamentArgs.Player1UserId,
                    SLOTNUMBER = HOST_SLOT_NUMBER,
                    ISHOST = true,
                    ISREADY = true,
                    JOINEDATUTC = tournamentArgs.NowUtc
                };

                var guest = new MATCH_PLAYER
                {
                    MATCH = match,
                    USERID = tournamentArgs.Player2UserId,
                    SLOTNUMBER = GUEST_SLOT_NUMBER,
                    ISHOST = false,
                    ISREADY = true,
                    JOINEDATUTC = tournamentArgs.NowUtc
                };

                dataContext.MATCH.Add(match);
                dataContext.MATCH_PLAYER.Add(host);
                dataContext.MATCH_PLAYER.Add(guest);

                return new TournamentMatchCreationResult(matchCode);
            }

            return default;
        }

        private long TryResolveMatchIdByCode(string matchCode)
        {
            string safeCode = (matchCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(safeCode))
            {
                return INVALID_MATCH_ID;
            }

            return dataContext.MATCH
                .AsNoTracking()
                .Where(m => m.MATCHCODE == matchCode)
                .Select(m => (long?)m.MATCHID)
                .FirstOrDefault() ?? INVALID_MATCH_ID;
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
