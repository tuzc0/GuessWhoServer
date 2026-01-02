using GuessWhoServerDomain.Domain.Models.Session;
using GuessWhoServices.Coordinators.InternalDtos;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface ILoginCoordinator
    {
        SessionLoginResult LoginAndInitializeSession(LoginArgs args);

        bool Logout(long userProfileId);
    }
}