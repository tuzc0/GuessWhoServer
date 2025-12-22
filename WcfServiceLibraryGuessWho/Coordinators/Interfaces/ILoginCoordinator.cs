using WcfServiceLibraryGuessWho.Coordinators.Parameters;
using GuessWhoContracts.Dtos.Dto;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface ILoginCoordinator
    {
        UserSessionLoginResult LoginAndInitializeSession(LoginArgs args);

        bool Logout(long userProfileId);
    }
}