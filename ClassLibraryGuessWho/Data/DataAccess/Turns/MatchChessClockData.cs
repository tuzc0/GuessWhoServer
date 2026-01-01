using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Turns;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Turns
{
    public sealed class MatchChessClockData : IMatchChessClockRepository
    {
        private const long INVALID_ID = 0;

        private const string PARAM_MATCH_ID = "@MatchId";
        private const string PARAM_USER_ID = "@UserId";
        private const string PARAM_NOW_UTC = "@NowUtc";
        private const string PARAM_MATCH_STATUS_ACTIVE = "@MatchStatusActive";

        private const byte MATCH_STATUS_ACTIVE_ID = (byte)MatchStatus.Active;

        private const string SQL_APPLY_SECONDS_ATOMIC =
            @"UPDATE MP
              SET MP.SECONDS_CONSUMED = MP.SECONDS_CONSUMED + DATEDIFF(SECOND, TS.TURNSTARTEDATUTC, @NowUtc)
              FROM dbo.MATCH_PLAYER MP
              INNER JOIN dbo.MATCH_TURN_STATE TS ON TS.MATCHID = MP.MATCHID
              INNER JOIN dbo.[MATCH] M ON M.MATCHID = MP.MATCHID
              WHERE MP.MATCHID = @MatchId
                AND MP.USERID = @UserId
                AND MP.LEFTATUTC IS NULL
                AND TS.CURRENTUSERID = @UserId
                AND M.STATUSID = @MatchStatusActive;";

        private const string SQL_GET_SECONDS_CONSUMED =
            @"SELECT MP.SECONDS_CONSUMED AS Value
              FROM dbo.MATCH_PLAYER MP
              WHERE MP.MATCHID = @MatchId
                AND MP.USERID = @UserId;";

        private const string SQL_GET_LIMIT_SECONDS =
            @"SELECT MM.TOTAL_TIME_LIMIT_SEC AS Value
              FROM dbo.[MATCH] M
              INNER JOIN dbo.MATCH_MODE MM ON MM.MODEID = M.MODEID
              WHERE M.MATCHID = @MatchId;";

        private const string SQL_GET_OPPONENT_USERID =
            @"SELECT TOP (1) MP.USERID AS Value
              FROM dbo.MATCH_PLAYER MP
              WHERE MP.MATCHID = @MatchId
                AND MP.LEFTATUTC IS NULL
                AND MP.USERID <> @UserId;";

        private const string SQL_DIAGNOSTIC =
            @"SELECT
                CASE WHEN EXISTS (SELECT 1 FROM dbo.[MATCH] M WHERE M.MATCHID = @MatchId) THEN 1 ELSE 0 END AS MatchExists,
                CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.[MATCH] M
                    WHERE M.MATCHID = @MatchId
                      AND M.STATUSID = @MatchStatusActive
                      AND M.STARTTIME IS NOT NULL
                      AND M.ENDTIME IS NULL
                ) THEN 1 ELSE 0 END AS MatchIsActive,
                CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.MATCH_TURN_STATE TS
                    WHERE TS.MATCHID = @MatchId AND TS.CURRENTUSERID = @UserId
                ) THEN 1 ELSE 0 END AS IsUsersTurn,
                CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.MATCH_PLAYER MP
                    WHERE MP.MATCHID = @MatchId AND MP.USERID = @UserId AND MP.LEFTATUTC IS NULL
                ) THEN 1 ELSE 0 END AS IsPlayerActive;";

        private readonly GuessWhoDBEntities dataContext;

        public MatchChessClockData(GuessWhoDBEntities dataContext)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public ApplyChessClockResult ApplyOnPassTurn(ApplyChessClockArgs chessClockArgs)
        {
            if (chessClockArgs == null || chessClockArgs.MatchId <= INVALID_ID || chessClockArgs.UserId <= INVALID_ID)
            {
                return ApplyChessClockResult.Fail(ApplyChessClockResultCode.InvalidArgs);
            }

            DateTime nowUtc = chessClockArgs.NowUtc == default ? DateTime.UtcNow : chessClockArgs.NowUtc;

            int affected = dataContext.Database.ExecuteSqlCommand(
                SQL_APPLY_SECONDS_ATOMIC,
                new SqlParameter(PARAM_MATCH_ID, chessClockArgs.MatchId),
                new SqlParameter(PARAM_USER_ID, chessClockArgs.UserId),
                new SqlParameter(PARAM_NOW_UTC, nowUtc),
                new SqlParameter(PARAM_MATCH_STATUS_ACTIVE, MATCH_STATUS_ACTIVE_ID));

            if (affected <= 0)
            {
                DiagnosticRow diagnostic = LoadDiagnostic(chessClockArgs.MatchId, chessClockArgs.UserId);

                if (!diagnostic.MatchExists)
                {
                    return ApplyChessClockResult.Fail(ApplyChessClockResultCode.MatchNotFound);
                }

                if (!diagnostic.MatchIsActive)
                {
                    return ApplyChessClockResult.Fail(ApplyChessClockResultCode.MatchNotInProgress);
                }

                if (!diagnostic.IsUsersTurn)
                {
                    return ApplyChessClockResult.Fail(ApplyChessClockResultCode.NotYourTurn);
                }

                if (!diagnostic.IsPlayerActive)
                {
                    return ApplyChessClockResult.Fail(ApplyChessClockResultCode.PlayerNotActive);
                }

                return ApplyChessClockResult.Fail(ApplyChessClockResultCode.OperationConflict);
            }

            int secondsConsumed = ReadScalarInt(
                SQL_GET_SECONDS_CONSUMED,
                new SqlParameter(PARAM_MATCH_ID, chessClockArgs.MatchId),
                new SqlParameter(PARAM_USER_ID, chessClockArgs.UserId));

            int limitSeconds = ReadScalarInt(
                SQL_GET_LIMIT_SECONDS,
                new SqlParameter(PARAM_MATCH_ID, chessClockArgs.MatchId));

            if (limitSeconds <= 0)
            {
                return ApplyChessClockResult.Fail(ApplyChessClockResultCode.OperationConflict);
            }

            long opponentUserId = ReadScalarLong(
                SQL_GET_OPPONENT_USERID,
                new SqlParameter(PARAM_MATCH_ID, chessClockArgs.MatchId),
                new SqlParameter(PARAM_USER_ID, chessClockArgs.UserId));

            return ApplyChessClockResult.Success(secondsConsumed, limitSeconds, opponentUserId);
        }

        private DiagnosticRow LoadDiagnostic(long matchId, long userId)
        {
            List<DiagnosticRow> rows = dataContext.Database.SqlQuery<DiagnosticRow>(
                SQL_DIAGNOSTIC,
                new SqlParameter(PARAM_MATCH_ID, matchId),
                new SqlParameter(PARAM_USER_ID, userId),
                new SqlParameter(PARAM_MATCH_STATUS_ACTIVE, MATCH_STATUS_ACTIVE_ID)).ToList();

            return rows.Count > 0 ? rows[0] : new DiagnosticRow();
        }

        private int ReadScalarInt(string sql, params SqlParameter[] parameters)
        {
            List<ScalarInt> rows = dataContext.Database.SqlQuery<ScalarInt>(sql, parameters).ToList();
            return rows.Count > 0 ? rows[0].Value : 0;
        }

        private long ReadScalarLong(string sql, params SqlParameter[] parameters)
        {
            List<ScalarLong> rows = dataContext.Database.SqlQuery<ScalarLong>(sql, parameters).ToList();
            return rows.Count > 0 ? rows[0].Value : 0;
        }

        private sealed class ScalarInt
        {
            public int Value { get; set; }
        }

        private sealed class ScalarLong
        {
            public long Value { get; set; }
        }

        private sealed class DiagnosticRow
        {
            public bool MatchExists { get; set; }
            public bool MatchIsActive { get; set; }
            public bool IsUsersTurn { get; set; }
            public bool IsPlayerActive { get; set; }
        }
    }
}
