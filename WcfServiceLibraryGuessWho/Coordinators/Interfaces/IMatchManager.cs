using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface IMatchManager
    {
        CreateMatchResponse CreateMatch(CreateMatchRequest request);
        JoinMatchResponse JoinMatch(JoinMatchRequest request);
        BasicResponse LeaveMatch(LeaveMatchRequest request);
        BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request);
        BasicResponse StartMatch(StartMatchRequest request);
        BasicResponse EndMatch(EndMatchRequest request);
        BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request);
        MatchDeckResponse GetMatchDeck(GetOrCreateMatchDeckRequest request);

        void SubscribeLobby(long matchId, IMatchCallback callbackChannel);
        void UnsubscribeLobby(long matchId, IMatchCallback callbackChannel);
    }
}
