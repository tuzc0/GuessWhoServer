using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GuessWho.Services.WCF.Security;
using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Contracts.Services;
using ClassLibraryGuessWho.Data;

namespace WcfServiceLibraryGuessWho.Services
{
    public class LoginService : ILoginService
    {
        public LoginResponse LoginUser(LoginRequest request)
        {
            using (var db = new GuessWhoDB())
            {
                var account = db.ACCOUNT
                    .Where(a => a.EMAIL == request.User)
                    .Select(a => new { a.PASSWORD, a.USERID })
                    .FirstOrDefault();

                if (account == null || !PasswordHasher.Verify(request.Password, account.PASSWORD))
                    return null;

                var profile = db.USER_PROFILE.FirstOrDefault(u => u.USERID == account.USERID);

                return new LoginResponse
                {
                    User = account.USERID.ToString(),
                    Password = profile?.DISPLAYNAME
                };
            }
        }

    }
}
