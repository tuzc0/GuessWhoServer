using WcfServiceLibraryGuessWho.Coordinators.Parameters;
using GuessWhoContracts.Dtos.Dto; 

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface ILoginManager
    {
        UserSessionLoginResult Login(LoginArgs loginArgs);
    }
}