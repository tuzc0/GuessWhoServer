namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public readonly record struct FinalGuessSnapshot(
        int MatchStatusId, 
        long CurrentTurnUserId,
        long OpponentUserId, 
        string OpponentSecretCharacterId);
}
