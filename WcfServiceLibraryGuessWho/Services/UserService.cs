using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.Helpers; 
using GuessWho.Contracts.Dtos;
using GuessWho.Contracts.Services;
using GuessWho.Services.WCF.Security;
using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false)]
    public class UserService : IUserService
    {
        public RegisterResponse RegisterUser(RegisterRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "Registration request cannot be null.");
            }

            var email = (request.Email ?? string.Empty).Trim();
            var displayName = (request.DisplayName ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;
            var nowUtc = DateTime.UtcNow;

            using (var dataBaseContext = new GuessWhoDB())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                try
                {
                    if (dataBaseContext.ACCOUNT.Any(a => a.EMAIL == email))
                    {
                        throw Faults.Create("DuplicateEmail", "Email already registered.");
                    }

                    var profile = new USER_PROFILE
                    {
                        DISPLAYNAME = displayName,
                        ISACTIVE = true,
                        CREATEDATUTC = nowUtc
                    };

                    var account = new ACCOUNT
                    {
                        USER_PROFILE = profile,
                        EMAIL = email,
                        PASSWORD = PasswordHasher.HashPassword(password),
                        ISEMAILVERIFIED = false,
                        CREATEDATUTC = nowUtc,
                        UPDATEDATUTC = nowUtc
                    };

                    dataBaseContext.ACCOUNT.Add(account);
                    dataBaseContext.SaveChanges();
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
                catch (FaultException<ServiceFault>)
                {
                    transaction.Rollback();
                    throw; 
                }
                catch (DbUpdateException ex) when (SqlExceptionInspector.IsForeignKeyViolation(ex))
                {
                    transaction.Rollback();
                    throw Faults.Create("ForeignKey", "Operation violates an existing relation.");
                }
                catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
                {
                    transaction.Rollback();
                    throw Faults.Create("DatabaseTimeout", "The database did not respond in time.");
                }
                catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
                {
                    transaction.Rollback();
                    throw Faults.Create("DatabaseConnection", "Unable to connect to the database.");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw Faults.Create("Unexpected", "Unexpected server error.");
                }
            }
        }
    }
}
