namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public sealed class ChangeSecretCharacterArgs
    {
        public long MatchId { get; }
        public long UserId { get; }
        public string SecretCharacterId { get; }

        public ChangeSecretCharacterArgs(long matchId, long userId, string secretCharacterId)
        {
            MatchId = matchId;
            UserId = userId;
            SecretCharacterId = secretCharacterId ?? string.Empty;
        }
    }
}
