namespace GuessWhoContracts.Enums
{
    public enum UserSessionLoginStatus
    {
        Success = 0,
        InvalidCredentials = 1, 
        AccountLocked = 2, 
        ProfileAlreadyActive = 3, 
        AccountNotFoundOrDeleted = 4, 
        ProfileNotFound = 5,
        LastLoginUpdateFailed = 6,
        Errored = 7
    }
}
