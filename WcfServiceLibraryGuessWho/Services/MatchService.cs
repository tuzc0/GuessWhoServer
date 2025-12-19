using ClassLibraryGuessWho.Data.DataAccess.Characters;
using ClassLibraryGuessWho.Data.DataAccess.Match;
using GuessWho.Services.WCF.Services.MatchApplication;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Services.MatchApplication;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = false)]
    public sealed class MatchService : IMatchService
    {
        private readonly IMatchCreationService matchCreationService;
        private readonly ILobbyCoordinator lobbyCoordinator;
        private readonly IMatchLifecycleService matchLifecycleService;

        public MatchService()
        {
            var matchData = new MatchData();

            var notifierLogger = LogManager.GetLogger(typeof(LobbyNotifier));
            var lobbyNotifier = new LobbyNotifier(notifierLogger);

            var characterData = new CharacterData();
            var characterDeckData = new CharacterDeckData();

            IMatchDeckProvider matchDeckProvider = new MatchDeckProvider(characterData, characterDeckData);

            matchCreationService = new MatchCreationService(matchData);
            lobbyCoordinator = new LobbyCoordinator(matchData, lobbyNotifier);
            matchLifecycleService = new MatchLifecycleService(
                matchData,
                lobbyNotifier,
                matchDeckProvider);
        }


        internal MatchService(
            IMatchCreationService matchCreationService, 
            ILobbyCoordinator lobbyCoordinator,
            IMatchLifecycleService matchLifecycleService)
        {
            this.matchCreationService = matchCreationService ?? 
                throw new ArgumentNullException(nameof(matchCreationService));
            
            this.lobbyCoordinator = lobbyCoordinator ?? 
                throw new ArgumentNullException(nameof(lobbyCoordinator));
            
            this.matchLifecycleService = matchLifecycleService ?? 
                throw new ArgumentNullException(nameof(matchLifecycleService));
        }

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            return matchCreationService.CreateMatch(request);
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            return lobbyCoordinator.JoinMatch(request);
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            return lobbyCoordinator.LeaveMatch(request);
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            return lobbyCoordinator.SetPlayerReadyStatus(request);
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            return matchLifecycleService.StartMatch(request);
        }

        public BasicResponse EndMatch(EndMatchRequest request)
        {
            return matchLifecycleService.EndMatch(request);
        }

        public BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request)
        {
            return matchLifecycleService.ChooseSecretCharacter(request);
        }

        public MatchDeckResponse GetMatchDeck(GetMatchDeckRequest request)
        {
            return matchLifecycleService.GetMatchDeck(request);
        }

        public void SubscribeLobby(long matchId)
        {
            lobbyCoordinator.SubscribeLobby(matchId);
        }

        public void UnsubscribeLobby(long matchId)
        {
            lobbyCoordinator.UnsubscribeLobby(matchId);
        }
    }
}
