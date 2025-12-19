namespace GuessWhoContracts.Enums
{
    public enum SqlErrorKind
    {
        None = 0,
        ForeignKeyViolation,
        UniqueViolation,
        Deadlock,
        DatabaseNotFound, 
        Timeout, 
        ConnectionFailure, 
        LoginFailed
    }
}
