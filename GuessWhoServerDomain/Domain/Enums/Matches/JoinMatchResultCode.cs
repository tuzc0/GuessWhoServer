namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum JoinMatchResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotJoinable = 2,
        PlayerAlreadyInMatch = 3,
        GuestSlotTaken = 4,
        InOtherActiveMatch = 5,
        OperationConflict = 6,
        InvalidArgs = 7
    }
}
