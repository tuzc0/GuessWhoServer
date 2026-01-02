using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface IUpdateProfileManager
    {
        ProfileSnapshot GetProfile(long userId);

        UpdatedProfileSnapshot UpdateUserProfile(UpdateProfileArgs args);

        bool DeleteUserProfile(long userId);
    }
}
