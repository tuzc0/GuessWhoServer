namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum JoinMatchResultCode
    {
        Sucess = 0,
        MatchNotFound = 1,
        MatchNotJoinable = 2,
        PlayerAlreadyInMatch = 3,
        GuestSlotTaken = 4,
        InOtherActiveMatch = 5,
        TechnicalError = 99
    }
}
