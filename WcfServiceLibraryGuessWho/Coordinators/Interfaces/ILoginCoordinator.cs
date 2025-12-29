using GuessWhoServerDomain.Domain.Models.Sessions;
using WcfServiceLibraryGuessWho.Coordinators.Parameters.InternalDtos;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface ILoginCoordinator
    {
        SessionLoginResult LoginAndInitializeSession(LoginArgs args);

        bool Logout(long userProfileId);
    }
}