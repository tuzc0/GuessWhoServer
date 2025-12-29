using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public interface IMatchCreationService
    {
        CreateMatchResponse CreateMatch(CreateMatchRequest request);
    }
}
