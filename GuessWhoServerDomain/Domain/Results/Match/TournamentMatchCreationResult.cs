namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct TournamentMatchCreationResult(string MatchCode)
    {
        public bool Isvalid => !string.IsNullOrWhiteSpace(MatchCode);
    }
}
