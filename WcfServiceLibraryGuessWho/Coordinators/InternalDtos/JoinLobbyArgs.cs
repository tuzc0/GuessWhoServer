namespace GuessWhoServices.Coordinators.InternalDtos
{
    public sealed class JoinLobbyArgs
    {
        public long UserId { get; init; }
        public string MatchCode { get; init; }
    }
}
