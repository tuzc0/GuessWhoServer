using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using System.Collections.Generic;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public interface IMatchData
    {
        MatchDto CreateMatchClassic(CreateMatchArgs args);
        StartMatchResult StartMatch(long matchId);
        EndMatchResult EndMatch(EndMatchArgs args);
        MatchDto GetOpenMatchByCode(string matchCode);
        JoinMatchResult AddPlayerToMatchByCode(JoinMatchArgs args);
        List<LobbyPlayerDto> GetMatchPlayers(long matchId);
        LeaveMatchResult LeaveMatch(MatchPlayerArgs args);
        MarkReadyResult MarkPlayerAsReady(MatchPlayerArgs args);
        ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs args);
        bool AreAllSecretCharactersChosen(long matchId);
        bool ForceLeaveAllMatchesForUser(long userId);
    }
}