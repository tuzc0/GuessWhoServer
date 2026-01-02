using GuessWhoServerDomain.Domain.Models.Session;
using GuessWhoServices.Coordinators.InternalDtos;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface ILoginManager
    {
        SessionLoginResult Login(LoginArgs loginArgs);
    }
}