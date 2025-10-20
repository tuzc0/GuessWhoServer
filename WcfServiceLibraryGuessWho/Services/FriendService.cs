using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
/*
namespace WcfServiceLibraryGuessWho.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class FriendService : IFriendService
    {
        private const byte PENDING_STATUS = 0;
        private const byte ACCEPTED_STATUS = 1;
        private const byte REJECTED_STATUS = 2;
        private const byte CANCELED_STATUS = 3;

        public SearchProfilesResponse SearchProfiles(SearchProfileRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "Registration request cannot be null.");
            }

            var displayName = (request.DisplayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw Faults.Create("InvalidDisplayName", "Display name cannot be empty.");
            }

            int maxResults = 10;

            using (var dataBaseContext = new ClassLibraryGuessWho.Data.GuessWhoDB())
            {
                
                var profiles = dataBaseContext.USER_PROFILE.AsNoTracking()
                    .Where(p => p.DISPLAYNAME.Contains(displayName))
                    .OrderBy(u => u.DISPLAYNAME)
                    .Take(maxResults)
                    .Select(p => new UserProfileSearchResult
                    {
                        UserId = p.USERID,
                        DisplayName = p.DISPLAYNAME,
                        AvatarUrl = p.AVATARURL
                    })
                    .ToList();
                
                return new SearchProfilesResponse
                {
                    Profiles = profiles
                };
            }
        }
    }
}*/
