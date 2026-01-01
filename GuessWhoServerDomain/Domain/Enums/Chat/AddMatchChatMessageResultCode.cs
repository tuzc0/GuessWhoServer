namespace GuessWhoServerDomain.Domain.Enums.Chat
{
    public enum AddMatchChatMessageResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotFound = 2,
        MatchNotJoinable = 3,
        SenderNotInMatch = 4,
        SenderAlreadyLeft = 5,
        OperationConflict = 6
    }
}
