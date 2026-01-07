using GuessWhoCore.Contracts.Faults;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Models.Leaderboard;
using GuessWhoServerDomain.Domain.Results.Leaderboard;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWhoServices.Coordinators
{
    public sealed class LeaderboardManager : ManagerBase, ILeaderboardManager
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(LeaderboardManager));

        private const string LOG_CTX_GET_GLOBAL = "LeaderboardManager.GetGlobalLeaderboard";
        private const int DEFAULT_TOP_N = 10;
        private const int MIN_VALID_ID = 1;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public LeaderboardManager(IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public LeaderboardResult GetGlobalLeaderboard(int topN, long requestingUserId)
        {
            return ExecuteService(
                LOG_CTX_GET_GLOBAL,
                () =>
                {
                    if (topN < 0)
                    {
                        throw FaultsFactory.Create(
                            LeaderboardFaultKeys.CODE_INVALID_TOP_N,
                            LeaderboardFaultKeys.MSG_INVALID_TOP_N,
                            LeaderboardFaultKeys.FALLBACK_INVALID_TOP_N);
                    }

                    int limit = topN == 0 ? DEFAULT_TOP_N : topN;

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    {
                        var topPlayers = unitOfWork.Leaderboards.GetTopWinners(limit);

                        LeaderboardPlayer currentUserStats = null;

                        if (requestingUserId >= MIN_VALID_ID)
                        {
                            EnsureUserActiveOrThrow(unitOfWork, requestingUserId);

                            currentUserStats = unitOfWork.Leaderboards.GetPlayerStats(requestingUserId);
                        }

                        return new LeaderboardResult
                        {
                            TopPlayers = topPlayers,
                            CurrentUserStats = currentUserStats
                        };
                    }
                });
        }

        private void EnsureUserActiveOrThrow(IGuessWhoUnitOfWork unitOfWork, long userId)
        {
            bool isActive = unitOfWork.Friendships.IsUserProfileActive(userId);

            if (isActive) return;

            throw FaultsFactory.Create(
                LeaderboardFaultKeys.CODE_USER_NOT_FOUND,
                LeaderboardFaultKeys.MSG_USER_NOT_FOUND,
                LeaderboardFaultKeys.FALLBACK_USER_NOT_FOUND);
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}