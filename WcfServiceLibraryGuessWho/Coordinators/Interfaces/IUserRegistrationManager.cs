using GuessWhoServices.Coordinators;
using GuessWhoServices.Coordinators.InternalDtos;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface IUserRegistrationManager
    {
        RegisterResult RegisterUser(RegisterUserArgs registerUserArgs);
    }
}
