using GuessWhoServerDomain.Domain.Models.Accounts;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IUserProfileRepository
    {
        UserProfileSnapshot GetUserProfileById(long userId);
        long AddUserProfile(UserProfileRecord userProfile);
    }
}