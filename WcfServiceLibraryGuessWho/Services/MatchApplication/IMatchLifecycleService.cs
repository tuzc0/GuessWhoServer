using GuessWhoContracts.Dtos.RequestAndResponse;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public interface IMatchLifecycleService
    {
        BasicResponse StartMatch(StartMatchRequest request);

        BasicResponse EndMatch(EndMatchRequest request);

        BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request);

        MatchDeckResponse GetMatchDeck(GetMatchDeckRequest request);

        // cambiar modo de juego.
    }

}
