using GuessWhoContracts.Dtos.Dto;
using System;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public interface ILobbyNotifier
    {
        void SubscribeLobby(long matchId);

        void UnsubscribeLobby(long matchId);

        void NotifyLobbyJoined(long matchId, LobbyPlayerDto player);

        void NotifyPlayerLeft(long matchId, LobbyPlayerDto player);

        void NotifyPlayerReadyStatusChanged(long matchId, LobbyPlayerDto player);

        void NotifyLobbyNotificationSafe(LobbyNotificationDto lobbyNotification,
            Action<long, LobbyPlayerDto> notifyAction);

        void NotifyGameStarted(long matchId);

        void NotifyGameEnded(long matchId, long winnerUserId);

        void NotifySecretCharacterChosen(long matchId, long userId);

        void NotifyAllSecretCharactersChosen(long matchId);
    }
}
