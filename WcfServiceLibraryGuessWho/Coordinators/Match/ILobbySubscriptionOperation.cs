using GuessWhoServices.Coordinators.InternalDtos;

namespace GuessWhoServices.Coordinators.Match
{
    public interface ILobbySubscriptionOperations
    {
        bool Subscribe(LobbySubscriptionArgs lobbySubscriptionArgs);
        bool Unsubscribe(LobbySubscriptionArgs lobbySubscriptionArgs);
    }
}
