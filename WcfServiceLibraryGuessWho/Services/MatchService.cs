using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Match;
using GuessWhoServices.Errors;
using GuessWhoServices.Infrastructure;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWhoServices.Services
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerCall,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = false)]
    public sealed partial class MatchService : ServiceBase, IMatchService
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(UserService));

        private const string CONTEXT_ASK_QUESTION = "MatchService.AskQuestion";
        private const string CONTEXT_ANSWER_QUESTION = "MatchService.AnswerQuestion";
        private const string CONTEXT_PASS_TURN = "MatchService.PassTurn";
        private const string CONTEXT_CLAIM_TIMEOUT = "MatchService.ClaimTimeout";
        private const string CONTEXT_FINAL_GUESS = "MatchService.FinalGuess";

        private const string CONTEXT_START_MATCH = "MatchService.StartMatch";
        private const string CONTEXT_END_MATCH = "MatchService.EndMatch";
        private const string CONTEXT_CHOOSE_SECRET = "MatchService.ChooseSecretCharacter";
        private const string CONTEXT_CHANGE_SECRET = "MatchService.ChangeSecretCharacter";
        private const string CONTEXT_GET_DECK = "MatchService.GetMatchDeck";

        private const string CONTEXT_CREATE_MATCH = "MatchService.CreateMatch";
        private const string CONTEXT_JOIN_MATCH = "MatchService.JoinMatch";
        private const string CONTEXT_LEAVE_MATCH = "MatchService.LeaveMatch";
        private const string CONTEXT_SET_READY = "MatchService.SetPlayerReadyStatus";
        private const string CONTEXT_SUBSCRIBE = "MatchService.SubscribeLobby";
        private const string CONTEXT_UNSUBSCRIBE = "MatchService.UnsubscribeLobby";

        private readonly MatchLobbyLogic lobbyLogic; 
        private readonly MatchLifecycleLogic lifecycleLogic;
        private readonly MatchDeckLogic deckLogic; 
        private readonly MatchSecretCharacterLogic secretCharacterLogic;
        private readonly MatchQuestionLogic questionLogic;
        private readonly MatchPassTurnLogic passTurnLogic;
        private readonly MatchGuessingLogic guessingLogic;
        private readonly IMatchCallbackDispatcher callbackDispatcher;

        public MatchService(MatchServiceDependencies dependencies)
        {
            lobbyLogic = dependencies.LobbyLogic ?? 
                throw new ArgumentNullException(nameof(lobbyLogic));
            lifecycleLogic = dependencies.LifecycleLogic ?? 
                throw new ArgumentNullException(nameof(lifecycleLogic));
            deckLogic = dependencies.DeckLogic ?? 
                throw new ArgumentNullException(nameof(deckLogic));
            secretCharacterLogic = dependencies.SecretCharacterLogic ??
                throw new ArgumentNullException(nameof(secretCharacterLogic));
            questionLogic = dependencies.QuestionLogic ??
                throw new ArgumentNullException(nameof(questionLogic));
            passTurnLogic = dependencies.PassTurnLogic ??
                throw new ArgumentNullException(nameof(passTurnLogic));
            guessingLogic = dependencies.GuessingLogic ??
                throw new ArgumentNullException(nameof(guessingLogic));
            callbackDispatcher = dependencies.CallbackDispatcher ??
                throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public sealed class MatchServiceDependencies
        {
            public MatchLobbyLogic LobbyLogic { get; init; }
            public MatchLifecycleLogic LifecycleLogic { get; init; }
            public MatchDeckLogic DeckLogic { get; init; }
            public MatchSecretCharacterLogic SecretCharacterLogic { get; init; }
            public MatchQuestionLogic QuestionLogic { get; init; }
            public MatchPassTurnLogic PassTurnLogic { get; init; }
            public MatchGuessingLogic GuessingLogic { get; init; }
            public IMatchCallbackDispatcher CallbackDispatcher { get; init; }
        }

        private static BasicResponse BasicOk()
        {
            return new BasicResponse
            {
                Success = true,
                Code = string.Empty,
                MeesageKey = string.Empty
            };
        }

        private static BasicResponse BasicFail(string code, string messageKey)
        {
            string safeCode = code ?? string.Empty;
            string safeKey = messageKey ?? safeCode;

            return new BasicResponse
            {
                Success = false,
                Code = safeCode ?? string.Empty,
                MeesageKey = safeKey ?? string.Empty
            };
        }

        private static void EnsureRequestNotNull(object request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                code: "REQUEST_NULL",
                messageKey: "Request.Null",
                fallbackMessage: "Request cannot be null");
        }

        private static TurnStateDto MapTurnState(TurnStateSnapshot snapshot)
        {
            return new TurnStateDto
            {
                MatchId = snapshot.MatchId,
                TurnNumber = snapshot.TurnNumber,
                CurrentPosition = snapshot.CurrentPosition,
                CurrentUserId = snapshot.CurrentUserId,
                TurnStartedAtUtc = snapshot.TurnStartedAtUtc
            };
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}