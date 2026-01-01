namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum JoinMatchOutcomeCode
    {
        Success, 
        MatchNotFound, 
        MatchNotJoinable, 
        PlayerAlreadyInMatch, 
        GuestSlotTaken, 
        InOtherActiveMatch, 
        TechnicalError
    }
}
