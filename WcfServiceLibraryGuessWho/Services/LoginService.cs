using System;
using System.Linq;
using System.ServiceModel;
using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Contracts.Services;
using ClassLibraryGuessWho.Data;
using GuessWho.Services.WCF.Security;

namespace WcfServiceLibraryGuessWho.Services
{
    public class LoginService : ILoginService
    {
        public LoginResponse LoginUser(LoginRequest request)
        {
            if (request == null)
                throw new FaultException("Invalid request.");

            string email = (request.User ?? string.Empty).Trim();
            string password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new FaultException("Email and password are required.");

            using (var db = new GuessWhoDB())
            {
                try
                {
                    var account = db.ACCOUNT.FirstOrDefault(a => a.EMAIL == email);

                    if (account == null || !PasswordHasher.Verify(password, account.PASSWORD))
                    {
                        return new LoginResponse
                        {
                            User = "Invalid",
                            Password = string.Empty,
                            ValidUser = "False"
                        };
                    }

                    var profile = db.USER_PROFILE.FirstOrDefault(u => u.USERID == account.USERID);

                    // Actualizar fecha de último acceso
                    account.LASTLOGINUTC = DateTime.UtcNow;
                    db.SaveChanges();

                    return new LoginResponse
                    {
                        User = profile?.DISPLAYNAME ?? "Unknown",
                        Password = account.EMAIL,
                        ValidUser = "True"
                    };
                }
                catch (Exception)
                {
                    throw new FaultException("Unexpected server error.");
                }
            }
        }
    }
}
