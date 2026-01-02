namespace GuessWhoServerDomain.Domain.Enums.Matches
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
