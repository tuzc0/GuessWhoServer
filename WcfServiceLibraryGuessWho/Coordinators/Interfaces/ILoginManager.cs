using GuessWhoServerDomain.Domain.Models.Sessions;
using WcfServiceLibraryGuessWho.Coordinators.Parameters.InternalDtos;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface ILoginManager
    {
        SessionLoginResult Login(LoginArgs loginArgs);
    }
}