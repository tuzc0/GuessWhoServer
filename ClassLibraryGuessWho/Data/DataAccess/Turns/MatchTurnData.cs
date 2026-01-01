using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Turns;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Turns
{
    public sealed class MatchTurnData : IMatchTurnRepository
    {
        private const int REQUIRED_PLAYERS = 2;
        private const byte FIRST_POSITION = 1;
        private const int FIRST_TURN_NUMBER = 1;

        private const string SQL_INITIALIZE_ATOMIC =
            @"DECLARE @CanInit BIT = 0;

              IF EXISTS (
                    SELECT 1
                    FROM dbo.[MATCH] M
                    WHERE M.MATCHID = @MatchId
                      AND M.STATUSID = @MatchStatusActive
                      AND M.STARTTIME IS NOT NULL
                      AND M.ENDTIME IS NULL
              )
              AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.MATCH_TURN_STATE S
                    WHERE S.MATCHID = @MatchId
              )
              AND (
                    SELECT COUNT(*)
                    FROM dbo.MATCH_PLAYER MP
                    WHERE MP.MATCHID = @MatchId
                      AND MP.LEFTATUTC IS NULL
                      AND MP.USERID IN (@UserId1, @UserId2)
              ) = @ExpectedPlayers
              AND (
                    SELECT COUNT(DISTINCT MP.USERID)
                    FROM dbo.MATCH_PLAYER MP
                    WHERE MP.MATCHID = @MatchId
                      AND MP.LEFTATUTC IS NULL
                      AND MP.USERID IN (@UserId1, @UserId2)
              ) = @ExpectedPlayers
              BEGIN
                    SET @CanInit = 1;
              END

              IF @CanInit = 1
              BEGIN
                    INSERT INTO dbo.MATCH_TURN_ORDER (MATCHID, POSITION, USERID, CREATEDATUTC)
                    VALUES (@MatchId, @Position1, @UserId1, @NowUtc);

                    INSERT INTO dbo.MATCH_TURN_ORDER (MATCHID, POSITION, USERID, CREATEDATUTC)
                    VALUES (@MatchId, @Position2, @UserId2, @NowUtc);

                    INSERT INTO dbo.MATCH_TURN_STATE (MATCHID, TURNNUMBER, CURRENTPOSITION, CURRENTUSERID, TURNSTARTEDATUTC, TURNEXPIRESATUTC)
                    VALUES (@MatchId, @TurnNumber, @StartPosition, @StartUserId, @NowUtc, NULL);
              END";

        private const string SQL_DIAGNOSTIC =@"SELECT
                CASE WHEN EXISTS (SELECT 1 FROM dbo.[MATCH] M WHERE M.MATCHID = @MatchId) THEN 1 ELSE 0 END AS MatchExists,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.[MATCH] M WHERE M.MATCHID = @MatchId AND M.STATUSID = @MatchStatusActive 
                   AND M.STARTTIME IS NOT NULL AND M.ENDTIME IS NULL) THEN 1 ELSE 0 END AS MatchIsActive,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.MATCH_TURN_STATE S WHERE S.MATCHID = @MatchId) THEN 1 ELSE 0 END AS TurnStateExists,
                (SELECT COUNT(*)
                 FROM dbo.MATCH_PLAYER MP
                 WHERE MP.MATCHID = @MatchId
                   AND MP.LEFTATUTC IS NULL
                   AND MP.USERID IN (@UserId1, @UserId2)) AS ActiveProvidedPlayersCount,
                (SELECT COUNT(DISTINCT MP.USERID)
                 FROM dbo.MATCH_PLAYER MP
                 WHERE MP.MATCHID = @MatchId
                   AND MP.LEFTATUTC IS NULL
                   AND MP.USERID IN (@UserId1, @UserId2)) AS DistinctProvidedPlayersCount;";

        private const string SQL_HAS_TURN_STATE = 
            @"SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.MATCH_TURN_STATE WHERE MATCHID = @MatchId) THEN 1 ELSE 0 END;";

        private const string SQL_GET_TURN_STATE =@"SELECT
                                                    MATCHID          AS MatchId,
                                                    TURNNUMBER       AS TurnNumber,
                                                    CURRENTPOSITION  AS CurrentPosition,
                                                    CURRENTUSERID    AS CurrentUserId,
                                                    TURNSTARTEDATUTC AS TurnStartedAtUtc
                                                    FROM dbo.MATCH_TURN_STATE
                                                    WHERE MATCHID = @MatchId;";

        private readonly GuessWhoDBEntities dataContext;

        public MatchTurnData(GuessWhoDBEntities dataContext)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public bool HasTurnState(long matchId)
        {
            if (matchId <= 0)
            {
                return false;
            }

            List<int> rows = dataContext.Database.SqlQuery<int>(
                    SQL_HAS_TURN_STATE,
                    new SqlParameter("@MatchId", matchId))
                .ToList();

            return rows.Count > 0 && rows[0] == 1;
        }

        public InitializeTurnOrderResult InitializeTurnOrder(InitializeTurnOrderArgs orderArgs)
        {
            InitializeTurnPlan plan = BuildInitializeTurnPlan(orderArgs);
            if (!plan.IsValid)
            {
                return InitializeTurnOrderResult.Fail(InitializeTurnOrderResultCode.InvalidArgs);
            }

            try
            {
                dataContext.Database.ExecuteSqlCommand(
                    SQL_INITIALIZE_ATOMIC,
                    new SqlParameter("@MatchId", plan.MatchId),
                    new SqlParameter("@MatchStatusActive", (byte)MatchStatus.Active),
                    new SqlParameter("@ExpectedPlayers", REQUIRED_PLAYERS),
                    new SqlParameter("@UserId1", plan.UserId1),
                    new SqlParameter("@UserId2", plan.UserId2),
                    new SqlParameter("@Position1", plan.Position1),
                    new SqlParameter("@Position2", plan.Position2),
                    new SqlParameter("@TurnNumber", plan.TurnNumber),
                    new SqlParameter("@StartPosition", plan.StartPosition),
                    new SqlParameter("@StartUserId", plan.StartUserId),
                    new SqlParameter("@NowUtc", plan.NowUtc));
            }
            catch (SqlException ex) when (SqlExceptionInspector.IsUniqueConstraintViolation(ex))
            {
                return InitializeTurnOrderResult.Fail(InitializeTurnOrderResultCode.OperationConflict);
            }

            if (HasTurnState(plan.MatchId))
            {
                return InitializeTurnOrderResult.Success();
            }

            InitializeTurnDiagnostic diagnostic = LoadInitializeTurnDiagnostic(plan);
            return MapInitializeDiagnosticToResult(diagnostic);
        }

        public TurnStateSnapshot GetTurnState(long matchId)
        {
            if (matchId <= 0)
            {
                return CreateInvalidSnapshot();
            }

            List<TurnStateRow> rows = dataContext.Database.SqlQuery<TurnStateRow>(
                    SQL_GET_TURN_STATE,
                    new SqlParameter("@MatchId", matchId))
                .ToList();

            if (rows.Count == 0)
            {
                return CreateInvalidSnapshot();
            }

            TurnStateRow row = rows[0];

            return new TurnStateSnapshot(
                row.MatchId,
                row.TurnNumber,
                row.CurrentPosition,
                row.CurrentUserId,
                row.TurnStartedAtUtc);
        }

        private static InitializeTurnPlan BuildInitializeTurnPlan(InitializeTurnOrderArgs args)
        {
            if (args == null || args.MatchId <= 0)
            {
                return InitializeTurnPlan.Invalid();
            }

            IReadOnlyList<long> userIds = args.UserIdsInOrder ?? Array.Empty<long>();
            if (userIds.Count != REQUIRED_PLAYERS)
            {
                return InitializeTurnPlan.Invalid();
            }

            long userId1 = userIds[0];
            long userId2 = userIds[1];

            if (userId1 <= 0 || userId2 <= 0 || userId1 == userId2)
            {
                return InitializeTurnPlan.Invalid();
            }

            DateTime nowUtc = args.NowUtc == default ? DateTime.UtcNow : args.NowUtc;

            return new InitializeTurnPlan(
                args.MatchId,
                userId1,
                userId2,
                Position1: FIRST_POSITION,
                Position2: (byte)(FIRST_POSITION + 1),
                TurnNumber: FIRST_TURN_NUMBER,
                StartPosition: FIRST_POSITION,
                StartUserId: userId1,
                NowUtc: nowUtc,
                IsValid: true);
        }

        private InitializeTurnDiagnostic LoadInitializeTurnDiagnostic(InitializeTurnPlan plan)
        {
            List<InitializeTurnDiagnostic> rows = dataContext.Database.SqlQuery<InitializeTurnDiagnostic>(
                    SQL_DIAGNOSTIC,
                    new SqlParameter("@MatchId", plan.MatchId),
                    new SqlParameter("@MatchStatusActive", (byte)MatchStatus.Active),
                    new SqlParameter("@UserId1", plan.UserId1),
                    new SqlParameter("@UserId2", plan.UserId2))
                .ToList();

            return rows.Count > 0 ? rows[0] : new InitializeTurnDiagnostic();
        }

        private static InitializeTurnOrderResult MapInitializeDiagnosticToResult(InitializeTurnDiagnostic diagnostic)
        {
            if (!diagnostic.MatchExists)
            {
                return InitializeTurnOrderResult.Fail(InitializeTurnOrderResultCode.MatchNotFound);
            }

            if (diagnostic.TurnStateExists)
            {
                return InitializeTurnOrderResult.Fail(InitializeTurnOrderResultCode.AlreadyInitialized);
            }

            if (!diagnostic.MatchIsActive)
            {
                return InitializeTurnOrderResult.Fail(InitializeTurnOrderResultCode.MatchNotInProgress);
            }

            bool playersOk =
                diagnostic.ActiveProvidedPlayersCount == REQUIRED_PLAYERS &&
                diagnostic.DistinctProvidedPlayersCount == REQUIRED_PLAYERS;

            if (!playersOk)
            {
                return InitializeTurnOrderResult.Fail(InitializeTurnOrderResultCode.PlayersNotInMatch);
            }

            return InitializeTurnOrderResult.Fail(InitializeTurnOrderResultCode.OperationConflict);
        }

        private static TurnStateSnapshot CreateInvalidSnapshot()
        {
            return new TurnStateSnapshot(
                matchId: 0,
                turnNumber: 0,
                currentPosition: 0,
                currentUserId: 0,
                turnStartedAtUtc: DateTime.MinValue);
        }

        private readonly record struct InitializeTurnPlan(
            long MatchId,
            long UserId1,
            long UserId2,
            byte Position1,
            byte Position2,
            int TurnNumber,
            byte StartPosition,
            long StartUserId,
            DateTime NowUtc,
            bool IsValid)
        {
            public static InitializeTurnPlan Invalid()
            {
                return new InitializeTurnPlan(0, 0, 0, 0, 0, 0, 0, 0, DateTime.MinValue, IsValid: false);
            }
        }

        private sealed class InitializeTurnDiagnostic
        {
            public bool MatchExists { get; set; }
            public bool MatchIsActive { get; set; }
            public bool TurnStateExists { get; set; }
            public int ActiveProvidedPlayersCount { get; set; }
            public int DistinctProvidedPlayersCount { get; set; }
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
