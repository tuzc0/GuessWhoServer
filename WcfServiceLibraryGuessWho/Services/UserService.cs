using System;
using System.Linq;
using System.ServiceModel;
using System.Data.Entity;
using System.Text.RegularExpressions;
using ClassLibraryGuessWho.Data;    
using GuessWho.Contracts.Dtos;
using GuessWho.Contracts.Services;
using GuessWho.Services.WCF.Security;

namespace GuessWho.Services.WCF.Services
{
    public class UserService : IUserService
    {
        public RegisterResponse RegisterUser(RegisterRequest request)
        {

            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var displayName = (request.DisplayName ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;
            var now = DateTime.UtcNow;

            using (var conectionDataBase = new GuessWhoDB())
            using (var transaction = conectionDataBase.Database.BeginTransaction())
            {

                if (!isEmailExists(email))
                {

                    Console.WriteLine("El correo electrónico ya está registrado."); // Se tiene que cambiar, solo fue para pruebas.
                }

            var profile = new USER_PROFILE
                {
                    DISPLAYNAME = displayName,
                    ISACTIVE = true,
                    CREATEDATUTC = now,
                    AVATARID = null
                };

                conectionDataBase.USER_PROFILE.Add(profile);
                conectionDataBase.SaveChanges();

                var passwordBytes = PasswordHasher.HashPassword(password);

                var account = new ACCOUNT
                {
                    USERID = profile.USERID,
                    EMAIL = email,
                    PASSWORD = passwordBytes,
                    ISEMAILVERIFIED = false,
                    LASTLOGINUTC = null,
                    FAILEDLOGINS = 0,
                    LOCKEDUNTILUTC = null,
                    CREATEDATUTC = now,
                    UPDATEDATUTC = now
                };

                conectionDataBase.ACCOUNT.Add(account);
                conectionDataBase.SaveChanges();

                transaction.Commit();

                return new RegisterResponse
                {
                    AccountId = account.ACCOUNTID,
                    UserId = profile.USERID,
                    Email = email,
                    DisplayName = displayName,
                    EmailVerificationRequired = true
                };
            }

        }

        private Boolean isEmailExists(string email)
        {
            using (var conectionDataBase = new GuessWhoDB())
            {
                var exists = conectionDataBase.ACCOUNT.Any(a => a.EMAIL == email);
                return exists;
            }
        }
    }
}


