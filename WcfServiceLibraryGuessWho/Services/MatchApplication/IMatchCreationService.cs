using GuessWhoContracts.Dtos.RequestAndResponse;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public interface IMatchCreationService
    {
        CreateMatchResponse CreateMatch(CreateMatchRequest request);
    }
}
