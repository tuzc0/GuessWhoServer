using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Accounts;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Profile
{
    public sealed class UserProfileData : IUserProfileRepository
    {
        private readonly GuessWhoDBEntities dataContext;

        public UserProfileData(GuessWhoDBEntities context)
        {
            this.dataContext = context;
        }

        public UserProfileSnapshot GetUserProfileById(long userId)
        {
            var profile = dataContext.USER_PROFILE.SingleOrDefault(p => p.USERID == userId);

            if (profile == null)
            {
                return UserProfileSnapshot.Invalid();
            }

            return new UserProfileSnapshot(
                profile.USERID,
                profile.DISPLAYNAME,
                profile.AVATARID,
                profile.ISGUEST
            );
        }
    }
}