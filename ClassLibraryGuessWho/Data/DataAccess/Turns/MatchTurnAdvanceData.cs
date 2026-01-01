using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Turns;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Turns
{
    public sealed class MatchTurnAdvanceData : IMatchTurnAdvanceRepository
    {
        private const long INVALID_ID = 0;

        private const byte FIRST_POSITION = 1;

        private const int ROWVERSION_BYTE_LENGTH = 8;

        private const string PARAM_MATCH_ID = "@MatchId";
        private const string PARAM_USER_ID = "@UserId";
        private const string PARAM_NOW_UTC = "@NowUtc";
        private const string PARAM_MATCH_STATUS_ACTIVE = "@MatchStatusActive";
        private const string PARAM_FIRST_POSITION = "@FirstPosition";
        private const string PARAM_EXPECTED_ROWVERSION = "@ExpectedRowVersion";

        private const byte MATCH_STATUS_ACTIVE_ID = (byte)MatchStatus.Active;

        private const string SQL_ADVANCE_TURN =
            @"DECLARE @MaxPos TINYINT;
              DECLARE @NextPos TINYINT;
              DECLARE @NextUserId BIGINT;

              SELECT @MaxPos = MAX([POSITION])
              FROM dbo.MATCH_TURN_ORDER
              WHERE MATCHID = @MatchId;

              IF @MaxPos IS NULL
              BEGIN
                  SELECT
                    CAST(0 AS BIGINT) AS MatchId,
                    CAST(0 AS INT) AS TurnNumber,
                    CAST(0 AS TINYINT) AS CurrentPosition,
                    CAST(0 AS BIGINT) AS CurrentUserId,
                    CAST('0001-01-01' AS DATETIME2(0)) AS TurnStartedAtUtc;
                  RETURN;
              END

              SELECT @NextPos =
                    CASE
                        WHEN TS.CURRENTPOSITION < @MaxPos THEN TS.CURRENTPOSITION + 1
                        ELSE @FirstPosition
                    END
              FROM dbo.MATCH_TURN_STATE TS
              WHERE TS.MATCHID = @MatchId;

              SELECT @NextUserId = USERID
              FROM dbo.MATCH_TURN_ORDER
              WHERE MATCHID = @MatchId AND [POSITION] = @NextPos;

              UPDATE TS
              SET TS.TURNNUMBER = TS.TURNNUMBER + 1,
                  TS.CURRENTPOSITION = @NextPos,
                  TS.CURRENTUSERID = @NextUserId,
                  TS.TURNSTARTEDATUTC = @NowUtc,
                  TS.TURNEXPIRESATUTC = DATEADD(SECOND, MM.DEFAULT_TURN_TIME_SEC, @NowUtc)
              OUTPUT
                  inserted.MATCHID AS MatchId,
                  inserted.TURNNUMBER AS TurnNumber,
                  inserted.CURRENTPOSITION AS CurrentPosition,
                  inserted.CURRENTUSERID AS CurrentUserId,
                  inserted.TURNSTARTEDATUTC AS TurnStartedAtUtc
              FROM dbo.MATCH_TURN_STATE TS
              INNER JOIN dbo.[MATCH] M ON M.MATCHID = TS.MATCHID
              INNER JOIN dbo.MATCH_MODE MM ON MM.MODEID = M.MODEID
              WHERE TS.MATCHID = @MatchId
                AND TS.CURRENTUSERID = @UserId
                AND M.STATUSID = @MatchStatusActive
                AND (@ExpectedRowVersion IS NULL OR TS.ROWVERSION = @ExpectedRowVersion);";

        private readonly GuessWhoDBEntities dataContext;

        public MatchTurnAdvanceData(GuessWhoDBEntities dataContext)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public AdvanceTurnResult AdvanceTurn(AdvanceTurnArgs turnArgs)
        {
            if (turnArgs == null || turnArgs.MatchId <= INVALID_ID || turnArgs.UserId <= INVALID_ID)
            {
                return AdvanceTurnResult.Fail(AdvanceTurnResultCode.InvalidArgs);
            }

            DateTime nowUtc = turnArgs.NowUtc == default ? DateTime.UtcNow : turnArgs.NowUtc;

            var expectedRowVersionParam = new SqlParameter(PARAM_EXPECTED_ROWVERSION, SqlDbType.VarBinary, ROWVERSION_BYTE_LENGTH)
            {
                Value = (object)turnArgs.ExpectedRowVersion ?? DBNull.Value
            };

            List<TurnStateRow> rows = dataContext.Database.SqlQuery<TurnStateRow>(
                SQL_ADVANCE_TURN,
                new SqlParameter(PARAM_MATCH_ID, turnArgs.MatchId),
                new SqlParameter(PARAM_USER_ID, turnArgs.UserId),
                new SqlParameter(PARAM_NOW_UTC, nowUtc),
                new SqlParameter(PARAM_MATCH_STATUS_ACTIVE, MATCH_STATUS_ACTIVE_ID),
                new SqlParameter(PARAM_FIRST_POSITION, FIRST_POSITION),
                expectedRowVersionParam).ToList();

            if (rows.Count == 0)
            {
                return AdvanceTurnResult.Fail(AdvanceTurnResultCode.OperationConflict);
            }

            TurnStateRow row = rows[0];

            if (row.MatchId <= INVALID_ID)
            {
                return AdvanceTurnResult.Fail(AdvanceTurnResultCode.TurnStateNotInitialized);
            }

            var snapshot = new TurnStateSnapshot(
                row.MatchId,
                row.TurnNumber,
                row.CurrentPosition,
                row.CurrentUserId,
                row.TurnStartedAtUtc);

            return AdvanceTurnResult.Success(snapshot);
        }

        private sealed class TurnStateRow
        {
            public long MatchId { get; set; }
            public int TurnNumber { get; set; }
            public byte CurrentPosition { get; set; }
            public long CurrentUserId { get; set; }
            public DateTime TurnStartedAtUtc { get; set; }
        }
    }
}
