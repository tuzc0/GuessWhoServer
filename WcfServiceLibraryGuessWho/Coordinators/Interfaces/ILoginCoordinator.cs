using GuessWhoServerDomain.Domain.Models.Session;
using GuessWhoServices.Coordinators.InternalDtos;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface ILoginCoordinator
    {
        SessionLoginResult LoginAndInitializeSession(LoginSessionArgs args);

        bool Logout(LogoutSessionArgs args);

        void TouchPresence(long userId);
    }
}