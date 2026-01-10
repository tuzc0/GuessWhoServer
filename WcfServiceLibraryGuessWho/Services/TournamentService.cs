using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Response;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Coordinators.Tournament;
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
    public sealed class TournamentService : ServiceBase, ITournamentService
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(TournamentService));

        private const string CONTEXT_HOST = "TournamentService.HostTournament";
        private const string CONTEXT_JOIN = "TournamentService.JoinTournament";
        private const string CONTEXT_SUBSCRIBE = "TournamentService.Subscribe";
        private const string CONTEXT_UNSUBSCRIBE = "TournamentService.Unsubscribe";

        private readonly TournamentLobbyLogic lobbyLogic;
        private readonly ITournamentSubscriptionOperations subscriptionOperations;

        public TournamentService(TournamentServiceDependencies dependencies)
        {
            this.lobbyLogic = dependencies.LobbyLogic ??
                throw new ArgumentNullException(nameof(lobbyLogic));
            this.subscriptionOperations = dependencies.SubscriptionOperations ??
                throw new ArgumentNullException(nameof(subscriptionOperations));
        }

        public sealed class TournamentServiceDependencies
        {
            public TournamentLobbyLogic LobbyLogic { get; init; }
            public ITournamentSubscriptionOperations SubscriptionOperations { get; init; }
        }

        public BasicResponse HostTournament(long hostUserId, int turnSeconds)
        {
            try
            {
                var response = lobbyLogic.HostTournament(hostUserId, turnSeconds);

                if (response.Success && long.TryParse(response.Code, out long tournamentId))
                {
                    SubscribeTournament(tournamentId, hostUserId);
                    response.Code = "OK";
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger.Error($"{CONTEXT_HOST}: Unexpected error.", ex);
                return new BasicResponse { Success = false, Code = "INTERNAL_ERROR" };
            }
        }

        public BasicResponse JoinTournament(long tournamentId, long userId)
        {
            try
            {
                SubscribeTournament(tournamentId, userId);

                return lobbyLogic.JoinTournament(tournamentId, userId);
            }
            catch (Exception ex)
            {
                Logger.Error($"{CONTEXT_JOIN}: Unexpected error.", ex);
                return new BasicResponse { Success = false, Code = "INTERNAL_ERROR" };
            }
        }

        public void SubscribeTournament(long tournamentId, long userId)
        {
            try
            {
                var callback = OperationContext.Current?.GetCallbackChannel<ITournamentCallback>();
                if (callback != null)
                {
                    subscriptionOperations.Subscribe(new TournamentSubscriptionArgs
                    {
                        TournamentId = tournamentId,
                        UserId = userId
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{CONTEXT_SUBSCRIBE}: Failed to subscribe user {userId} to tournament {tournamentId}", ex);
            }
        }

        public void UnsubscribeTournament(long tournamentId)
        {
            try
            {
                subscriptionOperations.Unsubscribe(new TournamentSubscriptionArgs
                {
                    TournamentId = tournamentId
                });
            }
            catch (Exception ex)
            {
                Logger.Warn($"{CONTEXT_UNSUBSCRIBE}: Error unsubscribing from tournament {tournamentId}", ex);
            }
        }

        protected override FaultException<GuessWhoCore.Contracts.Faults.ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}