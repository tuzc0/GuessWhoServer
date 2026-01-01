namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public readonly record struct FinalGuessSnapshot(
        int MatchStatusId, 
        long CurrentTurnUserId,
        long OpponentUserId, 
        string OpponentSecretCharacterId);
}
