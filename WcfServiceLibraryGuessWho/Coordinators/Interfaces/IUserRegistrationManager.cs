using WcfServiceLibraryGuessWho.Coordinators.Parameters;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface IUserRegistrationManager
    {
        RegisterResult RegisterUser(RegisterUserArgs registerUserArgs);
    }
}
