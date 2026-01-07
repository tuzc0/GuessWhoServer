using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Models.Leaderboard;
using GuessWhoServerDomain.Domain.Results.Leaderboard;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace GuessWhoServices.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false, InstanceContextMode = InstanceContextMode.PerCall)]
    public sealed class LeaderboardService : ServiceBase, ILeaderboardService
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(LeaderboardService));

        private const string LOG_CTX_GET_GLOBAL = "LeaderboardService.GetGlobalLeaderboard";
        private const string EMPTY = "";

        private readonly ILeaderboardManager leaderboardManager;

        public LeaderboardService(ILeaderboardManager leaderboardManager)
        {
            this.leaderboardManager = leaderboardManager ??
                throw new ArgumentNullException(nameof(leaderboardManager));
        }

        public GetLeaderboardResponse GetGlobalLeaderboard(GetLeaderboardRequest request)
        {
            return ExecuteService(
                LOG_CTX_GET_GLOBAL,
                () =>
                {
                    EnsureRequestNotNull(request);

                    LeaderboardResult result = leaderboardManager.GetGlobalLeaderboard(
                        request.TopN,
                        request.RequestingUserId);

                    return new GetLeaderboardResponse
                    {
                        Players = MapPlayerList(result.TopPlayers),
                        CurrentUserStats = MapPlayer(result.CurrentUserStats)
                    };
                });
        }

        private static void EnsureRequestNotNull(object request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                LeaderboardFaultKeys.CODE_REQUEST_NULL,
                LeaderboardFaultKeys.MSG_REQUEST_NULL,
                LeaderboardFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static List<LeaderboardPlayerDto> MapPlayerList(IList<LeaderboardPlayer> domainPlayers)
        {
            if (domainPlayers == null || domainPlayers.Count == 0)
            {
                return new List<LeaderboardPlayerDto>();
            }

            return domainPlayers.Select(MapPlayer).ToList();
        }

        private static LeaderboardPlayerDto MapPlayer(LeaderboardPlayer domainPlayer)
        {
            if (domainPlayer == null)
            {
                return null;
            }

            return new LeaderboardPlayerDto
            {
                Rank = domainPlayer.Rank,
                DisplayName = domainPlayer.DisplayName ?? EMPTY,
                AvatarId = domainPlayer.AvatarId ?? EMPTY,
                Wins = domainPlayer.Wins
            };
        }
    }
}