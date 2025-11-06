using ClassLibraryGuessWho.Contracts.Dtos;
using System.ServiceModel;

namespace ClassLibraryGuessWho.Contracts.Services
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
