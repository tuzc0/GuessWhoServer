namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    public sealed class ChooseSecretCharacterRequest
    {
        public long MatchId { get; set; }
        public long UserId { get; set; }
        public string CharacterId { get; set; }
    }
}
