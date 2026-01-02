using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Matches
{
    public sealed class MatchGuessingData : IMatchGuessingRepository
    {
        private const long INVALID_ID = 0;

        private const string PARAM_MATCH_ID = "@MatchId";
        private const string PARAM_USER_ID = "@UserId";

        private const string SQL_LOAD_FINAL_GUESS_SNAPSHOT =
            @"SELECT
                    M.STATUSID AS MatchStatusId,
                    ISNULL(TS.CURRENTUSERID, 0) AS CurrentTurnUserId,
                    ISNULL(OPP.USERID, 0) AS OpponentUserId,
                    OPP.SECRETCHARACTERID AS OpponentSecretCharacterId
              FROM dbo.[MATCH] M
              LEFT JOIN dbo.MATCH_TURN_STATE TS
                     ON TS.MATCHID = M.MATCHID
              OUTER APPLY (
                    SELECT TOP (1)
                           MP2.USERID,
                           MP2.SECRETCHARACTERID
                    FROM dbo.MATCH_PLAYER MP2
                    WHERE MP2.MATCHID = M.MATCHID
                      AND MP2.LEFTATUTC IS NULL
                      AND MP2.USERID <> @UserId
              ) OPP
              WHERE M.MATCHID = @MatchId;";

        private readonly GuessWhoDBEntities _dataContext;

        public MatchGuessingData(GuessWhoDBEntities dataContext)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public LoadFinalGuessSnapshotResult LoadFinalGuessSnapshot(long matchId, long guessingUserId)
        {
            if (matchId <= INVALID_ID || guessingUserId <= INVALID_ID)
            {
                return LoadFinalGuessSnapshotResult.Fail(LoadFinalGuessSnapshotResultCode.MatchNotFound);
            }

            List<FinalGuessRow> rows = _dataContext.Database.SqlQuery<FinalGuessRow>(
                SQL_LOAD_FINAL_GUESS_SNAPSHOT,
                new SqlParameter(PARAM_MATCH_ID, matchId),
                new SqlParameter(PARAM_USER_ID, guessingUserId)).ToList();

            if (rows.Count == 0)
            {
                return LoadFinalGuessSnapshotResult.Fail(LoadFinalGuessSnapshotResultCode.MatchNotFound);
            }

            FinalGuessRow row = rows[0];

            var snapshot = new FinalGuessSnapshot(
                row.MatchStatusId,
                row.CurrentTurnUserId,
                row.OpponentUserId,
                row.OpponentSecretCharacterId);

            return LoadFinalGuessSnapshotResult.Success(snapshot);
        }

        private sealed class FinalGuessRow
        {
            public int MatchStatusId { get; set; }
            public long CurrentTurnUserId { get; set; }
            public long OpponentUserId { get; set; }
            public string OpponentSecretCharacterId { get; set; }
        }
    }
}
