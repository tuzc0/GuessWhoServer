namespace GuessWhoServerDomain.Domain.Models.Match
{
    public sealed class CharacterRecord
    {
        public string CharacterId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
