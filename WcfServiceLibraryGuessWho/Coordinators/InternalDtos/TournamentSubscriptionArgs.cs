namespace GuessWhoServices.Coordinators.InternalDtos
{
    public sealed class TournamentSubscriptionArgs
    {
        public long TournamentId { get; init; }
        public long UserId { get; init; }
    }
}