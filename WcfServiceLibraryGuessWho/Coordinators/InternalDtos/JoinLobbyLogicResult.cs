using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Coordinators.InternalDtos
{
    public sealed class JoinLobbyLogicResult
    {
        public JoinLobbyLogicResult(
            JoinMatchResult result,
            MatchSnapshot match,
            IReadOnlyList<LobbyPlayerSnapshot> players,
            bool hasHostPlayer,
            LobbyPlayerSnapshot hostPlayer,
            bool hasJoinedPlayer,
            LobbyPlayerSnapshot joinedPlayer)
        {
            Result = result;
            Match = match;
            Players = players ?? Array.Empty<LobbyPlayerSnapshot>();
            HasHostPlayer = hasHostPlayer;
            HostPlayer = hostPlayer;
            HasJoinedPlayer = hasJoinedPlayer;
            JoinedPlayer = joinedPlayer;
        }

        public JoinMatchResult Result { get; }
        public MatchSnapshot Match { get; }
        public IReadOnlyList<LobbyPlayerSnapshot> Players { get; }

        public bool HasHostPlayer { get; }
        public LobbyPlayerSnapshot HostPlayer { get; }

        public bool HasJoinedPlayer { get; }
        public LobbyPlayerSnapshot JoinedPlayer { get; }
    }
}
