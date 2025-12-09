using ClassLibraryGuessWho.Data.DataAccess.Leaderboard;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using System;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class LeaderboardService : ILeaderboardService
    {

        private const string FAULT_CODE_REQUEST_NULL = "REQUEST_NULL";
        private const string FAULT_CODE_LEADERBOARD_INVALID_TOPN = "LEADERBOARD_INVALID_TOPN";
        private const string FAULT_CODE_LEADERBOARD_UNEXPECTED_ERROR = "LEADERBOARD_UNEXPECTED_ERROR";

        private const string FAULT_MESSAGE_REQUEST_NULL =
            "Request object cannot be null.";
        private const string FAULT_MESSAGE_LEADERBOARD_INVALID_TOPN =
            "TopN must be a non-negative number.";
        private const string FAULT_MESSAGE_LEADERBOARD_UNEXPECTED_ERROR =
            "An unexpected error occurred while retrieving leaderboard data.";

        private readonly LeaderboardData leaderboardData = new LeaderboardData();

        public GetLeaderboardResponse GetGlobalLeaderboard(GetLeaderboardRequest request)
        {
            EnsureRequestNotNull(request);

            if (request.TopN < 0)
            {
                throw Faults.Create(
                    FAULT_CODE_LEADERBOARD_INVALID_TOPN,
                    FAULT_MESSAGE_LEADERBOARD_INVALID_TOPN);
            }

            try
            {
                int limit = request.TopN == 0 ? 10 : request.TopN;
                var topPlayers = leaderboardData.GetTopWinners(limit);

                LeaderboardPlayerDto currentUserStats = null;

                if (request.RequestingUserId > 0)
                {
                    currentUserStats = leaderboardData.GetPlayerStats(request.RequestingUserId);
                }

                return new GetLeaderboardResponse
                {
                    Players = topPlayers,
                    CurrentUserStats = currentUserStats
                };
            }
            catch (Exception ex)
            {
                throw Faults.Create(
                    FAULT_CODE_LEADERBOARD_UNEXPECTED_ERROR,
                    FAULT_MESSAGE_LEADERBOARD_UNEXPECTED_ERROR,
                    ex);
            }
        }

        private static void EnsureRequestNotNull<T>(T request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }
        }
    }
}