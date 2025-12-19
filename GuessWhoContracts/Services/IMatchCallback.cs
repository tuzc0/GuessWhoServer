using GuessWhoContracts.Dtos.Dto;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IMatchCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnPlayerJoined(LobbyPlayerDto player);

        [OperationContract(IsOneWay = true)]
        void OnPlayerLeft(LobbyPlayerDto player);

        [OperationContract(IsOneWay = true)]
        void OnReadyChanged(LobbyPlayerDto player);

        [OperationContract(IsOneWay = true)]
        void OnSecretCharacterChosen(long matchId, long userId);

        [OperationContract(IsOneWay = true)]
        void OnAllSecretCharactersChosen(long matchId);

        [OperationContract(IsOneWay = true)]
        void OnGameStarted(long matchId);

        [OperationContract(IsOneWay = true)]
        void OnGameEnded(long matchId, long winnerUserId);
    }
}
