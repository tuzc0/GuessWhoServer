using ClassLibraryGuessWho.Data;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
namespace GuessWho.Services.WCF.Services
{
    public class ProfileService : IProfileService
    {
        public ProfileResponse Profile(ProfileRequest request)
        {
            if (request == null)
            {
                throw new FaultException("Invalid request.");
            }
            string email = (request.Email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new FaultException("Email is required.");
            }
            using (var databaseContext = new GuessWhoDBEntities())
            {
                try
                {
                    var user = databaseContext.ACCOUNT
                        .Include(a => a.USER_PROFILE)
                        .FirstOrDefault(a => a.EMAIL == email);
                    if (user == null)
                    {
                        throw new FaultException("Profile not found.");
                    }
                    var profile = user.USER_PROFILE;
                    return new ProfileResponse
                    {
                        Username = profile.DISPLAYNAME,
                        Email = user.EMAIL,
                        Password = "(hidden)"
                    };
                }
                catch (FaultException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new FaultException("Unexpected server error.");
                }
            }
        }
    }
}