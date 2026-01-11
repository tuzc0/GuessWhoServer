using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const string SQL_SET_MATCH_VISIBILITY_ATOMIC =
            @"UPDATE M
                SET M.VISIBILITYID = @TargetVisibility
                FROM [MATCH] M
                INNER JOIN [MATCH_PLAYER] HostMP
                   ON HostMP.MATCHID = M.MATCHID
                WHERE M.MATCHID = @MatchId
                    AND M.STATUSID = @MatchStatusLobby
                    AND M.STARTTIME IS NULL
                    AND M.ENDTIME IS NULL
                    AND M.VISIBILITYID <> @TargetVisibility
                    AND HostMP.USERID = @HostUserId
                    AND HostMP.ISHOST = 1
                    AND HostMP.LEFTATUTC IS NULL;";


        private sealed class SetPrivateDiagnostic
        {
            public bool MatchExists { get; set; }
            public byte MatchStatusId { get; set; }
            public byte VisibilityId { get; set; }
            public bool HasStartTime { get; set; }
            public bool HasEndTime { get; set; }
            public bool HostIsActiveHost { get; set; }
        }

        public MarkReadyResult MarkReady(MatchPlayerArgs playerArgs)
        {
            if (playerArgs == null)
            {
                throw new ArgumentNullException(nameof(playerArgs));
            }

            if (playerArgs.MatchId <= 0 || playerArgs.UserProfileId <= 0)
            {
                return MarkReadyResult.Fail(MarkReadyResultCode.InvalidArgs); 
            }

            MATCH_PLAYER playerEntity = dataContext.MATCH_PLAYER
                .Include(mp => mp.MATCH)
                .SingleOrDefault(mp =>
                    mp.MATCHID == playerArgs.MatchId &&
                    mp.USERID == playerArgs.UserProfileId);

            if (playerEntity == null)
            {
                return MarkReadyResult.Fail(MarkReadyResultCode.PlayerNotFound);
            }

            if (playerEntity.MATCH == null || !IsLobbyMatch(playerEntity.MATCH))
            {
                return MarkReadyResult.Fail(MarkReadyResultCode.MatchNotInLobby);
            }

            if (playerEntity.LEFTATUTC != null)
            {
                return MarkReadyResult.Fail(MarkReadyResultCode.PlayerAlreadyLeft);
            }

            playerEntity.ISREADY = true;

            return MarkReadyResult.Success();
        }

        public SetMatchVisibilityResult SetMatchVisibility(long matchId, long hostUserId, byte targetVisibilityId)
        {
            if (matchId <= 0 || hostUserId <= 0)
            {
                return SetMatchVisibilityResult.Fail(SetMatchVisibilityResultCode.InvalidArgs);
            }

            int rowsAffected = dataContext.Database.ExecuteSqlCommand(
                SQL_SET_MATCH_VISIBILITY_ATOMIC,
                new SqlParameter("@MatchId", matchId),
                new SqlParameter("@HostUserId", hostUserId),
                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY),
                new SqlParameter("@TargetVisibility", targetVisibilityId));

            if (rowsAffected > 0)
            {
                return SetMatchVisibilityResult.Success();
            }

            SetPrivateDiagnostic diagnostic =
                (from matchEntity in dataContext.MATCH.AsNoTracking()
                 where matchEntity.MATCHID == matchId
                 select new SetPrivateDiagnostic
                 {
                     MatchExists = true,
                     MatchStatusId = matchEntity.STATUSID,
                     VisibilityId = matchEntity.VISIBILITYID,
                     HasStartTime = matchEntity.STARTTIME != null,
                     HasEndTime = matchEntity.ENDTIME != null,
                     HostIsActiveHost = dataContext.MATCH_PLAYER.Any(mp =>
                         mp.MATCHID == matchId &&
                         mp.USERID == hostUserId &&
                         mp.ISHOST &&
                         mp.LEFTATUTC == null)
                 }).SingleOrDefault();

            if (diagnostic == null)
            {
                return SetMatchVisibilityResult.Fail(SetMatchVisibilityResultCode.MatchNotFound);
            }

            if (!diagnostic.HostIsActiveHost)
            {
                return SetMatchVisibilityResult.Fail(SetMatchVisibilityResultCode.HostNotAuthorized);
            }

            bool isNotLobby =
                diagnostic.MatchStatusId != MatchStatusIds.LOBBY ||
                diagnostic.HasStartTime ||
                diagnostic.HasEndTime;

            if (isNotLobby)
            {
                return SetMatchVisibilityResult.Fail(SetMatchVisibilityResultCode.MatchNotInLobby);
            }

            if (diagnostic.VisibilityId == targetVisibilityId)
            {
                return SetMatchVisibilityResult.Fail(SetMatchVisibilityResultCode.AlreadyInDesiredVisibility);
            }

            return SetMatchVisibilityResult.Fail(SetMatchVisibilityResultCode.ConcurrentUpdate);
        }
    }
}
