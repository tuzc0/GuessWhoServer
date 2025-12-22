using WcfServiceLibraryGuessWho.Coordinators.Parameters;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface ILoginManager
    {
        LoginResult Login(LoginArgs loginArgs);

        bool Logout(long userProfileId);
    }
}