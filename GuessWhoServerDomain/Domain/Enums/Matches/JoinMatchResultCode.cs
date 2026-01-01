namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum JoinMatchResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotJoinable = 2,
        PlayerAlreadyInMatch = 3,
        GuestSlotTaken = 4,
        InOtherActiveMatch = 5,
        OperationConflict = 6
    }
}
