using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServices.Services.MatchApplication;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Match;
using WcfServiceLibraryGuessWho.Errors;
using WcfServiceLibraryGuessWho.Services.MatchApplication;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed partial class MatchManager : ManagerBase, IMatchManager
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(MatchManager));
        protected override ILog Logger => logger;

        private const string CONTEXT_JOIN_MATCH = "MatchManager.JoinMatch";
        private const string CONTEXT_LEAVE_MATCH = "MatchManager.LeaveMatch";
        private const string CONTEXT_SET_READY = "MatchManager.SetPlayerReadyStatus";
        private const string CONTEXT_SUBSCRIBE = "MatchManager.SubscribeLobby";
        private const string CONTEXT_UNSUBSCRIBE = "MatchManager.UnsubscribeLobby";
        private const string CONTEXT_CREATE_MATCH = "MatchManager.CreateMatch"; 
        private const string CONTEXT_START_MATCH = "MatchManager.StartMatch";
        private const string CONTEXT_END_MATCH = "MatchManager.EndMatch";
        private const string CONTEXT_CHOOSE_SECRET_CHARACTER = "MatchManager.ChooseSecretCharacter";
        private const string CONTEXT_CHANGE_SECRET_CHARACTER = "MatchManager.ChangeSecretCharacter";
        private const string CONTEXT_GET_MATCH_DECK = "MatchManager.GetMatchDeck";
        private const string CONTEXT_CHANGE_PRIVATE_MATCH = "MatchManager.ChangePrivateMatch";
        private const string CONTEXT_SEARCH_PUBLIC_MATCH = "MatchManager.SearchPublicMatch";

        private readonly MatchLobbyLogic lobbyLogic;
        private readonly MatchLifecycleLogic lifecycleLogic;
        private readonly MatchDeckLogic deckLogic;
        private readonly MatchSecretCharacterLogic characterLogic;

        public MatchManager(
            MatchLobbyLogic lobbyLogic,
            MatchLifecycleLogic lifecycleLogic,
            MatchDeckLogic deckLogic,
            MatchSecretCharacterLogic characterLogic)
        {
            this.lobbyLogic = lobbyLogic ?? 
                throw new ArgumentNullException(nameof(lobbyLogic));
            this.lifecycleLogic = lifecycleLogic ?? 
                throw new ArgumentNullException(nameof(lifecycleLogic));
            this.deckLogic = deckLogic ?? 
                throw new ArgumentNullException(nameof(deckLogic));
            this.characterLogic = characterLogic ?? 
                throw new ArgumentNullException(nameof(characterLogic));
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            return ExecuteService(CONTEXT_JOIN_MATCH, () => lobbyLogic.JoinMatch(request));
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            return ExecuteService(CONTEXT_LEAVE_MATCH, () => lobbyLogic.LeaveMatch(request));
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            return ExecuteService(CONTEXT_SET_READY, () => lobbyLogic.SetPlayerReadyStatus(request));
        }

        public SearchPublicMatchResponse SearchPublicMatch(SearchPublicMatchRequest request)
        {
            return ExecuteService(CONTEXT_SEARCH_PUBLIC_MATCH, () => lifecycleLogic.SearchPublicMatch(request));
        }

        public void SubscribeLobby(long matchId, IMatchCallback callbackChannel)
        {
            ExecuteService(CONTEXT_SUBSCRIBE, () => lobbyLogic.SubscribeLobby(matchId, callbackChannel));
        }

        public void UnsubscribeLobby(long matchId, IMatchCallback callbackChannel)
        {
            ExecuteService(CONTEXT_UNSUBSCRIBE, () => lobbyLogic.UnsubscribeLobby(matchId, callbackChannel));
        }

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            return ExecuteService(CONTEXT_CREATE_MATCH, () => lifecycleLogic.CreateMatch(request));
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            return ExecuteService(CONTEXT_START_MATCH, () => lifecycleLogic.StartMatch(request));
        }

        public BasicResponse EndMatch(EndMatchRequest request)
        {
            return ExecuteService(CONTEXT_END_MATCH, () => lifecycleLogic.EndMatch(request));
        }

        public BasicResponse SetMatchPrivate(SetMatchPrivateRequest request)
        {
            return ExecuteService(CONTEXT_CHANGE_PRIVATE_MATCH, () => lifecycleLogic.SetMatchPrivate(request));
        }

        public BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request)
        {
            return ExecuteService(CONTEXT_CHOOSE_SECRET_CHARACTER, () => characterLogic.ChooseSecretCharacter(request));
        }

        public BasicResponse ChangeSecretCharacter(ChangeSecretCharacterRequest request)
        {
            return ExecuteService(CONTEXT_CHANGE_SECRET_CHARACTER, () => characterLogic.ChangeSecretCharacter(request));
        }

        public MatchDeckResponse GetMatchDeck(GetOrCreateMatchDeckRequest request)
        {
            return ExecuteService(CONTEXT_GET_MATCH_DECK, () => deckLogic.GetOrCreateMatchDeck(request));
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}