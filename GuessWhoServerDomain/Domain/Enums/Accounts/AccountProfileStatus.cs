namespace GuessWhoServerDomain.Domain.Enums.Accounts
{
    public enum AccountProfileStatus
    {
        Success = 0,
        NotFoundOrDeleted = 1,
        Locked = 2,
        ProfileNotFound = 3,
        ProfileAlreadyActive = 4
    }
}
