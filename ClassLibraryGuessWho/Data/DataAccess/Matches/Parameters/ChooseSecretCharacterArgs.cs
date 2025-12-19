namespace ClassLibraryGuessWho.Data.DataAccess.Match.Parameters
{
    public sealed class ChooseSecretCharacterArgs
    {
        public long MatchId { get; set; }

        public long UserProfileId { get; set; }

        public string SecretCharacterId { get; set; }
    }
}
