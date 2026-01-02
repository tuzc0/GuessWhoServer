namespace GuessWhoServices.Coordinators.InternalDtos
{
    public sealed class LobbySubscriptionArgs
    {
        public long MatchId { get; init; }
        public long UserId { get; init; }
    }
}
