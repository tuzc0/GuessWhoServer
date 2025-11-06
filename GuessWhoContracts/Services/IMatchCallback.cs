using GuessWhoContracts.Dtos.Dto;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    public interface IMatchCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnPlayerJoined(LobbyPlayerDto player);

        [OperationContract(IsOneWay = true)]
        void OnPlayerLeft(LobbyPlayerDto player);

        [OperationContract(IsOneWay = true)]
        void OnReadyChanged(LobbyPlayerDto player);
    }
}
