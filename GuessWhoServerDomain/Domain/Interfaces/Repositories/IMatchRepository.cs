using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchRepository
    {
        JoinMatchResult AddPlayerToMatchByCode(JoinMatchArgs matchArgs);
        JoinMatchResult AddPlayerToPublicMatchById(long matchId, long userProfileId);
        LeaveMatchResult LeaveMatch(MatchPlayerArgs playerArgs);
        DisconnectMatchResult HandleDisconnect(long userId, DateTime nowUtc);
        KickPlayerResult KickPlayer(KickPlayerArgs playerArgs);
        bool ForceLeaveAllMatchesForUser(long userId);

        MatchSnapshot CreateMatchClassic(CreateMatchArgs matchArgs);
        TournamentMatchCreationResult CreateMatchForTournamentQuick(CreateTournamentMatchArgs tournamentArgs);
        StartMatchResult StartMatch(long matchId, long hostUserId);
        EndMatchResult EndMatch(EndMatchArgs matchArgs);
        MarkReadyResult MarkReady(MatchPlayerArgs playerArgs);
        SetMatchPrivateResult SetMatchPrivate(long matchId, long hostUserId);

        ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs args);
        ChangeSecretCharacterResult ChangeSecretCharacter(ChangeSecretCharacterArgs args);
        bool AreAllSecretCharactersChosen(long matchId);

        MatchSnapshot GetOpenMatchByCode(string matchCode);
        MatchSnapshot GetMatchById(long matchId);
        IReadOnlyList<MatchSnapshot> GetPublicLobbyMatches();
        IReadOnlyList<LobbyPlayerSnapshot> GetMatchPlayers(long matchId);

        IReadOnlyList<long> GetActivePlayerIds(long matchId, int takeMax);
    }
}
