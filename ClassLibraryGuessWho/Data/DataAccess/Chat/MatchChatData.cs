using GuessWhoDataAccess.Data.Helpers;
using GuessWhoServerDomain.Domain.Enums.Chats;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Chat;
using GuessWhoServerDomain.Domain.Parameters.Chat;
using GuessWhoServerDomain.Domain.Results.Chat;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Chat
{
    public sealed class MatchChatData : IMatchChatRepository
    {
        private const int MESSAGE_MAX_LENGTH = 500;
        private const int MAX_TAKE_LAST = 150;

        private const string SQL_INSERT_MESSAGE_ATOMIC =
            @"INSERT INTO dbo.MATCH_CHAT_MESSAGE (MATCHID, SENDERUSERID, MESSAGE, CREATEDATUTC)
              SELECT
                    @MatchId,
                    @SenderUserId,
                    @Message,
                    @CreatedAtUtc
              WHERE EXISTS (
                    SELECT 1
                    FROM dbo.[MATCH] M
                    WHERE M.MATCHID = @MatchId
                      AND M.ENDTIME IS NULL
              )
              AND EXISTS (
                    SELECT 1
                    FROM dbo.MATCH_PLAYER MP
                    WHERE MP.MATCHID = @MatchId
                      AND MP.USERID = @SenderUserId
                      AND MP.LEFTATUTC IS NULL
              );";

        private const string SQL_SELECT_LAST_MESSAGES =
            @"SELECT TOP (@TakeLast)
                    MESSAGEID      AS MessageId,
                    MATCHID        AS MatchId,
                    SENDERUSERID   AS SenderUserId,
                    MESSAGE        AS Message,
                    CREATEDATUTC   AS CreatedAtUtc
              FROM dbo.MATCH_CHAT_MESSAGE
              WHERE MATCHID = @MatchId
              ORDER BY CREATEDATUTC DESC, MESSAGEID DESC;";

        private const string SQL_DIAGNOSTIC =
            @"SELECT
                CASE WHEN EXISTS (SELECT 1 FROM dbo.[MATCH] M WHERE M.MATCHID = @MatchId) THEN 1 ELSE 0 END AS MatchExists,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.[MATCH] M WHERE M.MATCHID = @MatchId AND M.ENDTIME IS NULL) THEN 1 ELSE 0 END AS MatchNotEnded,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.MATCH_PLAYER MP WHERE MP.MATCHID = @MatchId AND MP.USERID = @SenderUserId) THEN 1 ELSE 0 END AS SenderIsMember,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.MATCH_PLAYER MP WHERE MP.MATCHID = @MatchId AND MP.USERID = @SenderUserId AND MP.LEFTATUTC IS NULL) THEN 1 ELSE 0 END AS SenderIsActive;";

        private readonly GuessWhoDBEntities _dataContext;

        public MatchChatData(GuessWhoDBEntities dataContext)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public AddMatchChatMessageResult AddMessage(AddMatchChatMessageArgs chatMessageArgs)
        {
            AddMessagePlan plan = BuildAddMessagePlan(chatMessageArgs);
            if (!plan.IsValid)
            {
                return AddMatchChatMessageResult.Fail(AddMatchChatMessageResultCode.InvalidArgs);
            }

            int rowsAffected;
            try
            {
                rowsAffected = _dataContext.Database.ExecuteSqlCommand(
                    SQL_INSERT_MESSAGE_ATOMIC,
                    new SqlParameter("@MatchId", plan.MatchId),
                    new SqlParameter("@SenderUserId", plan.SenderUserId),
                    new SqlParameter("@Message", plan.Message),
                    new SqlParameter("@CreatedAtUtc", plan.CreatedAtUtc));
            }
            catch (SqlException ex) when (SqlExceptionInspector.IsUniqueConstraintViolation(ex))
            {
                return AddMatchChatMessageResult.Fail(AddMatchChatMessageResultCode.OperationConflict);
            }

            if (rowsAffected > 0)
            {
                return AddMatchChatMessageResult.Success();
            }

            AddMessageDiagnostic diagnostic = LoadAddMessageDiagnostic(plan);
            return MapDiagnosticToResult(diagnostic);
        }

        public IReadOnlyList<MatchChatMessageRecord> GetMessages(long matchId, int takeLast)
        {
            if (matchId <= 0)
            {
                return Array.Empty<MatchChatMessageRecord>();
            }

            int safeTakeLast = NormalizeTakeLast(takeLast);
            if (safeTakeLast <= 0)
            {
                return Array.Empty<MatchChatMessageRecord>();
            }

            List<ChatMessageRow> rows = _dataContext.Database.SqlQuery<ChatMessageRow>(
                    SQL_SELECT_LAST_MESSAGES,
                    new SqlParameter("@MatchId", matchId),
                    new SqlParameter("@TakeLast", safeTakeLast))
                .ToList();

            if (rows.Count == 0)
            {
                return Array.Empty<MatchChatMessageRecord>();
            }

            rows.Reverse();

            List<MatchChatMessageRecord> mapped = new List<MatchChatMessageRecord>(rows.Count);
            foreach (ChatMessageRow row in rows)
            {
                mapped.Add(new MatchChatMessageRecord(
                    row.MessageId,
                    row.MatchId,
                    row.SenderUserId,
                    row.Message ?? string.Empty,
                    row.CreatedAtUtc));
            }

            return mapped;
        }

        private static int NormalizeTakeLast(int takeLast)
        {
            if (takeLast <= 0)
            {
                return 0;
            }

            return takeLast > MAX_TAKE_LAST ? MAX_TAKE_LAST : takeLast;
        }

        private static AddMessagePlan BuildAddMessagePlan(AddMatchChatMessageArgs args)
        {
            if (args == null)
            {
                return AddMessagePlan.Invalid();
            }

            if (args.MatchId <= 0 || args.SenderUserId <= 0)
            {
                return AddMessagePlan.Invalid();
            }

            string message = (args.Message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                return AddMessagePlan.Invalid();
            }

            if (message.Length > MESSAGE_MAX_LENGTH)
            {
                message = message.Substring(0, MESSAGE_MAX_LENGTH);
            }

            DateTime createdAtUtc = args.CreatedAtUtc == default ? DateTime.UtcNow : args.CreatedAtUtc;

            return new AddMessagePlan(
                args.MatchId,
                args.SenderUserId,
                message,
                createdAtUtc,
                IsValid: true);
        }

        private AddMessageDiagnostic LoadAddMessageDiagnostic(AddMessagePlan plan)
        {
            List<AddMessageDiagnostic> rows = _dataContext.Database.SqlQuery<AddMessageDiagnostic>(
                    SQL_DIAGNOSTIC,
                    new SqlParameter("@MatchId", plan.MatchId),
                    new SqlParameter("@SenderUserId", plan.SenderUserId))
                .ToList();

            return rows.Count > 0 ? rows[0] : new AddMessageDiagnostic();
        }

        private static AddMatchChatMessageResult MapDiagnosticToResult(AddMessageDiagnostic diagnostic)
        {
            if (!diagnostic.MatchExists)
            {
                return AddMatchChatMessageResult.Fail(AddMatchChatMessageResultCode.MatchNotFound);
            }

            if (!diagnostic.MatchNotEnded)
            {
                return AddMatchChatMessageResult.Fail(AddMatchChatMessageResultCode.MatchNotJoinable);
            }

            if (!diagnostic.SenderIsMember)
            {
                return AddMatchChatMessageResult.Fail(AddMatchChatMessageResultCode.SenderNotInMatch);
            }

            if (!diagnostic.SenderIsActive)
            {
                return AddMatchChatMessageResult.Fail(AddMatchChatMessageResultCode.SenderAlreadyLeft);
            }

            return AddMatchChatMessageResult.Fail(AddMatchChatMessageResultCode.OperationConflict);
        }

        private readonly record struct AddMessagePlan(
            long MatchId,
            long SenderUserId,
            string Message,
            DateTime CreatedAtUtc,
            bool IsValid)
        {
            public static AddMessagePlan Invalid()
            {
                return new AddMessagePlan(0, 0, string.Empty, DateTime.MinValue, IsValid: false);
            }
        }

        private sealed class AddMessageDiagnostic
        {
            public bool MatchExists { get; set; }
            public bool MatchNotEnded { get; set; }
            public bool SenderIsMember { get; set; }
            public bool SenderIsActive { get; set; }
        }

        private sealed class ChatMessageRow
        {
            public long MessageId { get; set; }
            public long MatchId { get; set; }
            public long SenderUserId { get; set; }
            public string Message { get; set; }
            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
