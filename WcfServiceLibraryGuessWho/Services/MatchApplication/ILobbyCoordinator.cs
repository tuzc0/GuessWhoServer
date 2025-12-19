using GuessWhoContracts.Dtos.RequestAndResponse;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public interface ILobbyCoordinator
    {
        JoinMatchResponse JoinMatch(JoinMatchRequest request);

        BasicResponse LeaveMatch(LeaveMatchRequest request);

        BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request);

        // lista lobbys publicos

        void SubscribeLobby(long matchId);

        void UnsubscribeLobby(long matchId);
    }
}
