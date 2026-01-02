using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using log4net;
using System;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;

namespace GuessWhoServices.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = false)]
    public sealed class MatchService : ServiceBase, IMatchService
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(UserService));

        private const string CONTEXT_CREATE_MATCH = "MatchService.CreateMatch";
        private const string CONTEXT_JOIN_MATCH = "MatchService.JoinMatch";
        private const string CONTEXT_LEAVE_MATCH = "MatchService.LeaveMatch";
        private const string CONTEXT_SET_READY = "MatchService.SetPlayerReadyStatus";
        private const string CONTEXT_START_MATCH = "MatchService.StartMatch";
        private const string CONTEXT_END_MATCH = "MatchService.EndMatch";
        private const string CONTEXT_CHOOSE_SECRET = "MatchService.ChooseSecretCharacter";
        private const string CONTEXT_GET_DECK = "MatchService.GetMatchDeck";
        private const string CONTEXT_SUBSCRIBE = "MatchService.SubscribeLobby";
        private const string CONTEXT_UNSUBSCRIBE = "MatchService.UnsubscribeLobby";

        private readonly IMatchManager matchManager; 

        public MatchService(IMatchManager matchManager)
        {
            this.matchManager = matchManager ?? 
                throw new ArgumentNullException(nameof(matchManager));
        }

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            return ExecuteService(CONTEXT_CREATE_MATCH, () => matchManager.CreateMatch(request));
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            return ExecuteService(CONTEXT_JOIN_MATCH, () => matchManager.JoinMatch(request));
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            return ExecuteService(CONTEXT_LEAVE_MATCH, () => matchManager.LeaveMatch(request));
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            return ExecuteService(CONTEXT_SET_READY, () => matchManager.SetPlayerReadyStatus(request));
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            return ExecuteService(CONTEXT_START_MATCH, () => matchManager.StartMatch(request));
        }

        public BasicResponse EndMatch(EndMatchRequest request)
        {
            return ExecuteService(CONTEXT_END_MATCH, () => matchManager.EndMatch(request));
        }

        public BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request)
        {
            return ExecuteService(CONTEXT_CHOOSE_SECRET, () => matchManager.ChooseSecretCharacter(request));
        }

        public MatchDeckResponse GetMatchDeck(GetOrCreateMatchDeckRequest request)
        {
            return ExecuteService(CONTEXT_GET_DECK, () => matchManager.GetMatchDeck(request));
        }

        public void SubscribeLobby(long matchId)
        {
            ExecuteService(CONTEXT_SUBSCRIBE, () =>
            {
                IMatchCallback callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
                matchManager.SubscribeLobby(matchId, callbackChannel);
            });
        }

        public void UnsubscribeLobby(long matchId)
        {
            ExecuteService(CONTEXT_UNSUBSCRIBE, () =>
            {
                IMatchCallback callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
                matchManager.UnsubscribeLobby(matchId, callbackChannel);
            });
        }
    }
}