using GuessWhoServices.Coordinators.InternalDtos;

namespace GuessWhoServices.Coordinators.Match
{
    public interface ILobbySubscriptionOperations
    {
        void Subscribe(LobbySubscriptionArgs lobbySubscriptionArgs);
        void Unsubscribe(LobbySubscriptionArgs lobbySubscriptionArgs);
    }
}
