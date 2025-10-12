using ClassLibraryGuessWho.Data;
using GuessWho.Contracts.Dtos;
using GuessWho.Contracts.Services;
using GuessWho.Services.WCF.Security;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services
{
    public class UserService : IUserService
    {
        public RegisterResponse RegisterUser(RegisterRequest registrationRequest)
        {

            if (registrationRequest == null)
            {
                throw new FaultException("Invalid request.");
            }

            string email = (registrationRequest.Email ?? string.Empty).Trim();
            string displayName = (registrationRequest.DisplayName ?? string.Empty).Trim();
            string password = registrationRequest.Password ?? string.Empty;

            DateTime currentUtcDateTime = DateTime.UtcNow;

            using (var databaseContext = new GuessWhoDB())
            using (var databaseTransaction = databaseContext.Database.BeginTransaction())
            {
                try
                {
                    bool emailAlreadyExists = databaseContext.ACCOUNT.Any(account => account.EMAIL == email);

                    if (emailAlreadyExists)
                    {
                        throw new FaultException("Email already registered.");
                    }

                    var newUserProfile = new USER_PROFILE
                    {
                        DISPLAYNAME = displayName,
                        ISACTIVE = true,
                        CREATEDATUTC = currentUtcDateTime,
                        AVATARID = null
                    };

                    byte[] hashedPassword = PasswordHasher.HashPassword(password);

                    var newUserAccount = new ACCOUNT
                    {
                        USER_PROFILE = newUserProfile,
                        EMAIL = email,
                        PASSWORD = hashedPassword,
                        ISEMAILVERIFIED = false,
                        LASTLOGINUTC = null,
                        FAILEDLOGINS = 0,
                        LOCKEDUNTILUTC = null,
                        CREATEDATUTC = currentUtcDateTime,
                        UPDATEDATUTC = currentUtcDateTime
                    };

                    databaseContext.ACCOUNT.Add(newUserAccount);
                    databaseContext.SaveChanges();
                    databaseTransaction.Commit();

                    return new RegisterResponse
                    {
                        AccountId = newUserAccount.ACCOUNTID,
                        UserId = newUserProfile.USERID,
                        Email = email,
                        DisplayName = displayName,
                        EmailVerificationRequired = true
                    };
                }
                catch (DbUpdateException databaseUpdateException)
                {
                    databaseTransaction.Rollback();
                    if (IsUniqueConstraintViolation(databaseUpdateException))
                        throw new FaultException("Email already registered.");
                    throw new FaultException("Unexpected database error.");
                }
                catch (FaultException)
                {
                    databaseTransaction.Rollback();
                    throw;
                }
                catch (Exception)
                {
                    databaseTransaction.Rollback();
                    throw new FaultException("Unexpected server error.");
                }
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException databaseUpdateException)
        {
            var sqlException = databaseUpdateException.InnerException?.InnerException as System.Data.SqlClient.SqlException;
            return sqlException != null && (sqlException.Number == 2627 || sqlException.Number == 2601);
        }
    }
}
