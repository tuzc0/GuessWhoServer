using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Coordinators.InternalDtos
{
    public sealed class LeaveLobbyLogicResult
    {
        public LeaveLobbyLogicResult(
            LeaveMatchResult result,
            bool wasHost,
            IReadOnlyList<LobbyPlayerSnapshot> playersAfter)
        {
            Result = result;
            WasHost = wasHost;
            PlayersAfter = playersAfter ?? Array.Empty<LobbyPlayerSnapshot>();
        }

        public LeaveMatchResult Result { get; }
        public bool WasHost { get; }
        public IReadOnlyList<LobbyPlayerSnapshot> PlayersAfter { get; }
    }
}
