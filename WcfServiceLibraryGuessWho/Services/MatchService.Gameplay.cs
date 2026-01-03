using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Turns;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuessWhoServices.Services
{
    public partial class MatchService
    {
        public BasicResponse StartMatch(StartMatchRequest request)
        {
            return ExecuteService(CONTEXT_START_MATCH, () =>
            {
                EnsureRequestNotNull(request);

                StartMatchResult result = lifecycleLogic.StartMatch(request.MatchId, request.UserId);

                return result.IsSuccess 
                ? BasicOk()
                : BasicFail(result.Code.ToString(), result.Code.ToString());
            });
        }

        public BasicResponse EndMatch(EndMatchRequest request)
        {
            return ExecuteService(CONTEXT_END_MATCH, () =>
            {
                EnsureRequestNotNull(request);

                EndMatchResult result = lifecycleLogic.EndMatch(request.MatchId);

                return result.IsSuccess
                ? BasicOk()
                : BasicFail(result.Code.ToString(), result.Code.ToString());
            });
        }

        public BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request)
        {
            return ExecuteService(CONTEXT_CHOOSE_SECRET, () =>
            {
                EnsureRequestNotNull(request);

                ChooseSecretCharacterResult result = secretCharacterLogic.ChooseSecretCharacter(
                    new ChooseSecretCharacterArgs
                    {
                        MatchId = request.MatchId,
                        UserProfileId = request.UserId,
                        SecretCharacterId = request.CharacterId
                    });

                return result.IsSuccess
                ? BasicOk()
                : BasicFail(result.Code.ToString(), result.Code.ToString());
            });
        }

        public BasicResponse ChangeSecretCharacter(ChangeSecretCharacterRequest request)
        {
            return ExecuteService(CONTEXT_CHANGE_SECRET, () =>
            {
                EnsureRequestNotNull(request);

                ChangeSecretCharacterResult result = secretCharacterLogic.ChangeSecretCharacter(
                    new ChangeSecretCharacterArgs
                    (
                        request.MatchId,
                        request.UserId,
                        request.CharacterId
                    ));

                return result.IsSuccess
                ? BasicOk()
                : BasicFail(result.Code.ToString(), result.Code.ToString());
            });
        }

        public MatchDeckResponse GetMatchDeck(GetOrCreateMatchDeckRequest request)
        {
            return ExecuteService(CONTEXT_GET_DECK, () =>
            {
                EnsureRequestNotNull(request);

                MatchDeckResult result = deckLogic.GetOrCreateMatchDeck(request);

                return new MatchDeckResponse
                {
                    Success = result.IsSuccess,
                    Code = result.Code.ToString(),
                    CharacterIds = result.IsSuccess
                    ? (result.CharacterDeckIds?.ToList() ?? new List<string>())
                    : new List<string>()
                };
            });
        }

        public BasicResponse AskQuestion(AskQuestionRequest request)
        {
            return ExecuteService(CONTEXT_ASK_QUESTION, () =>
            {
                EnsureRequestNotNull(request);

                AskQuestionResult result = questionLogic.AskQuestion(
                    new AskQuestionArgs
                    {
                        MatchId = request.MatchId,
                        UserId = request.UserId,
                        AttributeId = request.AttributeId,
                        NowUtc = DateTime.UtcNow
                    });

                return result.IsSuccess
                ? BasicOk()
                : BasicFail(result.Code.ToString(), result.Code.ToString());
            });
        }

        public BasicResponse AnswerQuestion(AnswerQuestionRequest request)
        {
            return ExecuteService(CONTEXT_ANSWER_QUESTION, () =>
            {
                EnsureRequestNotNull(request);

                AnswerQuestionResult result = questionLogic.AnswerQuestion(
                    new AnswerQuestionArgs
                    {
                        MatchId = request.MatchId,
                        UserId = request.UserId,
                        AnswerOptionId = request.AnswerOptionId,
                        NowUtc = DateTime.UtcNow
                    });

                return result.IsSuccess
                ? BasicOk()
                : BasicFail(result.Code.ToString(), result.Code.ToString());
            });
        }

        public PassTurnResponse PassTurn(PassTurnRequest request)
        {
            return ExecuteService(CONTEXT_PASS_TURN, () =>
            {
                EnsureRequestNotNull(request);

                PassTurnOutcome outcome = passTurnLogic.PassTurn(
                    new PassTurnArgs
                    {
                        MatchId = request.MatchId,
                        UserId = request.UserId,
                        NowUtc = DateTime.UtcNow,
                        ExceptedRowVersion = request.ExpectedRowVersion
                    });

                if (!outcome.IsSuccess)
                {
                    return new PassTurnResponse
                    {
                        Success = false,
                        Code = outcome.Code.ToString(),
                        MeesageKey = outcome.Code.ToString(),
                        TurnState = new TurnStateDto(),
                        SecondsConsumed = 0,
                        LimitSeconds = 0,
                        IsTimeOut = false,
                        WinnerUserId = 0
                    };
                }

                return new PassTurnResponse
                {
                    Success = true,
                    Code = string.Empty,
                    MeesageKey = string.Empty,
                    TurnState = outcome.TurnState.MatchId > 0 ? MapTurnState(outcome.TurnState)
                    : new TurnStateDto(),
                    SecondsConsumed = outcome.SecondsConsumed,
                    LimitSeconds = outcome.LimitSeconds,
                    IsTimeOut = outcome.Code == PassTurnOutcomeCode.TimeOut,
                    WinnerUserId = outcome.WinnerUserId
                };
            });
        }

        public ClaimTimeoutResponse ClaimTimeout(ClaimTimeoutRequest request)
        {
            return ExecuteService(CONTEXT_CLAIM_TIMEOUT, () =>
            {
                EnsureRequestNotNull(request);

                ClaimTimeoutResult result = passTurnLogic.ClaimTimeout(
                    new ClaimTimeoutArgs
                    {
                        MatchId = request.MatchId,
                        UserId = request.UserId,
                        NowUtc = DateTime.UtcNow
                    });

                return new ClaimTimeoutResponse
                {
                    Success = result.IsSuccess,
                    Code = result.Code.ToString(),
                    MeesageKey = result.Code.ToString(),
                    MatchId = result.MatchId,
                    WinnerUserId = result.WinnerUserId,
                    TimedOutUserId = result.TimedOutUserId,
                    TotalSeconds = result.TotalSeconds,
                    LimitSeconds = result.LimitSeconds
                };
            });
        }

        public FinalGuessResponse FinalGuess(FinalGuessRequest request)
        {
            return ExecuteService(CONTEXT_FINAL_GUESS, () =>
            {
                EnsureRequestNotNull(request);

                FinalGuessResult result = guessingLogic.GuessOpponentCharacter(
                    request.MatchId,
                    request.UserId,
                    request.GuessedCharacterId);

                return new FinalGuessResponse
                {
                    Success = result.IsSuccess,
                    Code = result.Code.ToString(),
                    MeesageKey = result.Code.ToString(),
                    IsCorrect = result.IsCorrect,
                    WinnerUserId = 0
                };
            });
        }
    }
}
