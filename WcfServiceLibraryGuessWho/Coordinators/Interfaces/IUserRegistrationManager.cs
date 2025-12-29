using WcfServiceLibraryGuessWho.Coordinators.InternalDtos;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface IUserRegistrationManager
    {
        RegisterResult RegisterUser(RegisterUserArgs registerUserArgs);
    }
}
