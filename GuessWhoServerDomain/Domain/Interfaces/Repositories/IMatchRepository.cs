using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchRepository
    {
        MatchSnapshot CreateMatchClassic(CreateMatchArgs matchArgs);
        StartMatchResult StartMatch(long matchId); 
        EndMatchResult EndMatch(EndMatchArgs matchArgs);
        bool ForceLeaveAllMatchesForUser(long userId);

        MatchSnapshot GetOpenMatchByCode(string matchCode);
        IReadOnlyList<MatchSnapshot> GetPublicLobbyMatches();
        IReadOnlyList<LobbyPlayerSnapshot> GetMatchPlayers(long matchId);

        JoinMatchResult AddPlayerToMatchByCode(JoinMatchArgs matchArgs);
        JoinMatchResult AddPlayerToPublicMatchById(long matchId, long userId);

        LeaveMatchResult LeaveMatch(MatchPlayerArgs matchArgs);
        KickPlayerResult KickPlayer(KickPlayerArgs playerArgs);

        MarkReadyResult MarkReady(MatchPlayerArgs playerArgs);
        SetMatchPrivateResult SetMatchPrivate(long matchId, long hostUserId);

        ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs chooseSecretCharacterArgs);
        ChangeSecretCharacterResult ChangeSecretCharacter(ChangeSecretCharacterArgs changeSecretCharacterArgs);
        bool AreAllSecretCharactersChosen(long matchId);

        long CreateMatchForTournamentQuick(long player1UserId, long player2UserId, DateTime nowUtc);
    }
}
