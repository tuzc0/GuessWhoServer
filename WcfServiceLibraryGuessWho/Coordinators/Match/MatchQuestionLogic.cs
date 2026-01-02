using GuessWhoContracts.Services;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Chats;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Chat;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Chat;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Chat;
using GuessWhoServerDomain.Domain.Results.Turns;
using GuessWhoServices.Infrastructure;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Coordinators.Match
{
    public sealed class MatchQuestionLogic
    {
        private const long INVALID_ID = 0;

        private const int PHASE_SCAN_TAKE_LAST = 150;

        private const string QUESTION_PREFIX = "Q|";
        private const string ANSWER_PREFIX = "A|";

        private readonly IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory;
        private readonly IMatchCallbackDispatcher matchCallbackDispatcher;

        public MatchQuestionLogic(
            IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory,
            IMatchCallbackDispatcher matchCallbackDispatcher)
        {
            this.guessWhoUnitOfWorkFactory = guessWhoUnitOfWorkFactory ??
                throw new ArgumentNullException(nameof(guessWhoUnitOfWorkFactory));

            this.matchCallbackDispatcher = matchCallbackDispatcher ??
                throw new ArgumentNullException(nameof(matchCallbackDispatcher));
        }

        public AskQuestionResult AskQuestion(AskQuestionArgs args)
        {
            AskQuestionPlan plan = BuildAskPlan(args);
            if (!plan.IsValid)
            {
                return AskQuestionResult.Fail(AskQuestionResultCode.InvalidArgs);
            }

            using IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create();

            TurnStateSnapshot turnState = unitOfWork.MatchesTurns.GetTurnState(plan.MatchId);
            if (!IsValidTurnState(turnState))
            {
                return AskQuestionResult.Fail(AskQuestionResultCode.MatchNotFound);
            }

            if (turnState.CurrentUserId != plan.UserId)
            {
                return AskQuestionResult.Fail(AskQuestionResultCode.NotYourTurn);
            }

            TurnPhase phase = ResolvePhaseFromChat(unitOfWork.MatchesChats, plan.MatchId);
            if (phase != TurnPhase.Main)
            {
                return AskQuestionResult.Fail(AskQuestionResultCode.InvalidPhase);
            }

            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            AddMatchChatMessageResult addResult = unitOfWork.MatchesChats.AddMessage(new AddMatchChatMessageArgs
            {
                MatchId = plan.MatchId,
                SenderUserId = plan.UserId,
                Message = BuildQuestionMessage(plan.AttributeId),
                CreatedAtUtc = plan.NowUtc
            });

            if (!addResult.IsSuccess)
            {
                transaction.Rollback();
                return AskQuestionResult.Fail(MapAskChatFailure(addResult.Code));
            }

            unitOfWork.Flush();
            transaction.Commit();

            matchCallbackDispatcher.Broadcast(plan.MatchId, callback =>
                callback.OnQuestionAsked(plan.MatchId, plan.UserId, plan.AttributeId));

            return AskQuestionResult.Success();
        }

        public AnswerQuestionResult AnswerQuestion(AnswerQuestionArgs args)
        {
            AnswerQuestionPlan plan = BuildAnswerPlan(args);
            if (!plan.IsValid)
            {
                return AnswerQuestionResult.Fail(AnswerQuestionResultCode.InvalidArgs);
            }

            using IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create();

            TurnStateSnapshot turnState = unitOfWork.MatchesTurns.GetTurnState(plan.MatchId);
            if (!IsValidTurnState(turnState))
            {
                return AnswerQuestionResult.Fail(AnswerQuestionResultCode.MatchNotFound);
            }

            TurnPhase phase = ResolvePhaseFromChat(unitOfWork.MatchesChats, plan.MatchId);
            if (phase != TurnPhase.WaitingForResponse)
            {
                return AnswerQuestionResult.Fail(AnswerQuestionResultCode.InvalidPhase);
            }

            long askingUserId = turnState.CurrentUserId;
            if (askingUserId <= INVALID_ID)
            {
                return AnswerQuestionResult.Fail(AnswerQuestionResultCode.OperationConflict);
            }

            if (plan.UserId == askingUserId)
            {
                return AnswerQuestionResult.Fail(AnswerQuestionResultCode.NotOpponent);
            }

            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            AddMatchChatMessageResult addResult = unitOfWork.MatchesChats.AddMessage(new AddMatchChatMessageArgs
            {
                MatchId = plan.MatchId,
                SenderUserId = plan.UserId,
                Message = BuildAnswerMessage(plan.AnswerOptionId),
                CreatedAtUtc = plan.NowUtc
            });

            if (!addResult.IsSuccess)
            {
                transaction.Rollback();
                return AnswerQuestionResult.Fail(MapAnswerChatFailure(addResult.Code));
            }

            AdvanceTurnResult advanceResult = unitOfWork.MatchTurnAdvances.AdvanceTurn(new AdvanceTurnArgs
            {
                MatchId = plan.MatchId,
                UserId = askingUserId,
                NowUtc = plan.NowUtc,
                ExpectedRowVersion = null
            });

            if (!advanceResult.IsSuccess)
            {
                transaction.Rollback();
                return AnswerQuestionResult.Fail(AnswerQuestionResultCode.TurnAdvanceConflict);
            }

            unitOfWork.Flush();
            transaction.Commit();

            matchCallbackDispatcher.Broadcast(plan.MatchId, callback =>
                callback.OnQuestionAnswered(plan.MatchId, plan.UserId, plan.AnswerOptionId));

            return AnswerQuestionResult.Success();
        }

        private static AskQuestionPlan BuildAskPlan(AskQuestionArgs args)
        {
            if (args == null)
            {
                return AskQuestionPlan.Invalid();
            }

            if (args.MatchId <= INVALID_ID || args.UserId <= INVALID_ID || args.AttributeId <= 0)
            {
                return AskQuestionPlan.Invalid();
            }

            return new AskQuestionPlan(args.MatchId, args.UserId, args.AttributeId, DateTime.UtcNow, IsValid: true);
        }

        private static AnswerQuestionPlan BuildAnswerPlan(AnswerQuestionArgs args)
        {
            if (args == null)
            {
                return AnswerQuestionPlan.Invalid();
            }

            if (args.MatchId <= INVALID_ID || args.UserId <= INVALID_ID || args.AnswerOptionId <= 0)
            {
                return AnswerQuestionPlan.Invalid();
            }

            return new AnswerQuestionPlan(args.MatchId, args.UserId, args.AnswerOptionId, DateTime.UtcNow, IsValid: true);
        }

        private static bool IsValidTurnState(TurnStateSnapshot snapshot)
        {
            return snapshot.MatchId > INVALID_ID && snapshot.CurrentUserId > INVALID_ID;
        }

        private static string BuildQuestionMessage(int attributeId)
        {
            return QUESTION_PREFIX + attributeId.ToString();
        }

        private static string BuildAnswerMessage(int answerOptionId)
        {
            return ANSWER_PREFIX + answerOptionId.ToString();
        }

        private static TurnPhase ResolvePhaseFromChat(IMatchChatRepository matchChatRepository, long matchId)
        {
            IReadOnlyList<MatchChatMessageRecord> messages = matchChatRepository.GetMessages(matchId, PHASE_SCAN_TAKE_LAST);

            long lastQuestionMessageId = 0;
            long lastAnswerMessageId = 0;

            for (int i = messages.Count - 1; i >= 0; i--)
            {
                string message = messages[i].Message ?? string.Empty;

                if (lastQuestionMessageId == 0 && message.StartsWith(QUESTION_PREFIX, StringComparison.Ordinal))
                {
                    lastQuestionMessageId = messages[i].MessageId;
                }

                if (lastAnswerMessageId == 0 && message.StartsWith(ANSWER_PREFIX, StringComparison.Ordinal))
                {
                    lastAnswerMessageId = messages[i].MessageId;
                }

                if (lastQuestionMessageId > 0 && lastAnswerMessageId > 0)
                {
                    break;
                }
            }

            if (lastQuestionMessageId == 0)
            {
                return TurnPhase.Main;
            }

            if (lastAnswerMessageId == 0)
            {
                return TurnPhase.WaitingForResponse;
            }

            return lastQuestionMessageId > lastAnswerMessageId
                ? TurnPhase.WaitingForResponse
                : TurnPhase.Main;
        }

        private static AskQuestionResultCode MapAskChatFailure(AddMatchChatMessageResultCode code)
        {
            switch (code)
            {
                case AddMatchChatMessageResultCode.MatchNotFound:
                    return AskQuestionResultCode.MatchNotFound;

                case AddMatchChatMessageResultCode.SenderNotInMatch:
                case AddMatchChatMessageResultCode.SenderAlreadyLeft:
                    return AskQuestionResultCode.PlayerNotActive;

                default:
                    return AskQuestionResultCode.OperationConflict;
            }
        }

        private static AnswerQuestionResultCode MapAnswerChatFailure(AddMatchChatMessageResultCode code)
        {
            switch (code)
            {
                case AddMatchChatMessageResultCode.MatchNotFound:
                    return AnswerQuestionResultCode.MatchNotFound;

                case AddMatchChatMessageResultCode.SenderNotInMatch:
                case AddMatchChatMessageResultCode.SenderAlreadyLeft:
                    return AnswerQuestionResultCode.PlayerNotActive;

                default:
                    return AnswerQuestionResultCode.OperationConflict;
            }
        }

        private readonly record struct AskQuestionPlan(
            long MatchId,
            long UserId,
            int AttributeId,
            DateTime NowUtc,
            bool IsValid)
        {
            public static AskQuestionPlan Invalid()
            {
                return new AskQuestionPlan(0, 0, 0, DateTime.MinValue, IsValid: false);
            }
        }

        private readonly record struct AnswerQuestionPlan(
            long MatchId,
            long UserId,
            int AnswerOptionId,
            DateTime NowUtc,
            bool IsValid)
        {
            public static AnswerQuestionPlan Invalid()
            {
                return new AnswerQuestionPlan(0, 0, 0, DateTime.MinValue, IsValid: false);
            }
        }
    }
}
