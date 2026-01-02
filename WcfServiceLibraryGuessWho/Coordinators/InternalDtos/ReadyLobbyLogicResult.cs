using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Coordinators.InternalDtos
{
    public sealed class ReadyLobbyLogicResult
    {
        public ReadyLobbyLogicResult(
            MarkReadyResult result,
            IReadOnlyList<LobbyPlayerSnapshot> playersAfter,
            bool hasReadyPlayer,
            LobbyPlayerSnapshot readyPlayer)
        {
            Result = result;
            PlayersAfter = playersAfter ?? Array.Empty<LobbyPlayerSnapshot>();
            HasReadyPlayer = hasReadyPlayer;
            ReadyPlayer = readyPlayer;
        }

        public MarkReadyResult Result { get; }
        public IReadOnlyList<LobbyPlayerSnapshot> PlayersAfter { get; }

        public bool HasReadyPlayer { get; }
        public LobbyPlayerSnapshot ReadyPlayer { get; }
    }
}
