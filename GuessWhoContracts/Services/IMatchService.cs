using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IMatchCallback))]
    public interface IMatchService : IMatchLobbyOperations, IMatchGameplayOperations
    {
    }
}
