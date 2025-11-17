using GuessWhoContracts.Dtos.Dto;
using System;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts
{
    public sealed class UserAccountData
    {
        public bool EmailExists(string email)
        {
            bool emailExists = false;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                emailExists = dataBaseContext.ACCOUNT.Any(ua => ua.EMAIL == email);
            }

            return emailExists;
        }

        public (AccountDto account, UserProfileDto profile) CreateAccount(CreateAccountArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var profileEntity = new USER_PROFILE
                {
                    DISPLAYNAME = args.DisplayName,
                    ISACTIVE = true,
                    CREATEDATUTC = args.CreationDate
                };

                dataBaseContext.USER_PROFILE.Add(profileEntity);
                dataBaseContext.SaveChanges();

                var accountEntity = new ACCOUNT
                {
                    USERID = profileEntity.USERID,
                    EMAIL = args.Email,
                    PASSWORD = args.Password,
                    ISEMAILVERIFIED = false,
                    CREATEDATUTC = args.CreationDate,
                    UPDATEDATUTC = args.CreationDate
                };

                dataBaseContext.ACCOUNT.Add(accountEntity);
                dataBaseContext.SaveChanges();

                transaction.Commit();

                var accountDto = new AccountDto
                {
                    AccountId = accountEntity.ACCOUNTID,
                    UserId = accountEntity.USERID,
                    Email = accountEntity.EMAIL,
                    PasswordHash = accountEntity.PASSWORD,
                    IsEmailVerified = accountEntity.ISEMAILVERIFIED,
                    CreatedAtUtc = accountEntity.CREATEDATUTC,
                    UpdatedAtUtc = accountEntity.UPDATEDATUTC,
                    LastLoginUtc = accountEntity.LASTLOGINUTC
                };

                var profileDto = new UserProfileDto
                {
                    UserId = profileEntity.USERID,
                    DisplayName = profileEntity.DISPLAYNAME,
                    IsActive = profileEntity.ISACTIVE,
                    CreatedAtUtc = profileEntity.CREATEDATUTC
                };

                return (accountDto, profileDto);
            }
        }

        public bool GetAccountByIdAccount(long accountId, out AccountDto account)
        {
            account = null;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == accountId);

                if (accountEntity == null)
                {
                    return false;
                }

                account = new AccountDto
                {
                    AccountId = accountEntity.ACCOUNTID,
                    UserId = accountEntity.USERID,
                    Email = accountEntity.EMAIL,
                    PasswordHash = accountEntity.PASSWORD,
                    IsEmailVerified = accountEntity.ISEMAILVERIFIED,
                    CreatedAtUtc = accountEntity.CREATEDATUTC,
                    UpdatedAtUtc = accountEntity.UPDATEDATUTC,
                    LastLoginUtc = accountEntity.LASTLOGINUTC
                };

                return true;
            }
        }

        public void MarkEmailVerified(long accountId, DateTime nowUtc)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var emailVerified = dataBaseContext.ACCOUNT.Single(a => a.ACCOUNTID == accountId);

                emailVerified.ISEMAILVERIFIED = true;
                emailVerified.UPDATEDATUTC = nowUtc;
                dataBaseContext.SaveChanges();
            }
        }

        public bool TryGetAccountWithProfileByEmail(LoginAccountArgs args, out AccountDto account, out UserProfileDto profile)
        {
            account = null;
            profile = null;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.EMAIL == args.Email);

                if (accountEntity == null)
                {
                    return false;
                }

                var profileEntity = dataBaseContext.USER_PROFILE
                    .SingleOrDefault(p => p.USERID == accountEntity.USERID);

                if (profileEntity == null)
                {
                    return false;
                }

                account = new AccountDto
                {
                    AccountId = accountEntity.ACCOUNTID,
                    UserId = accountEntity.USERID,
                    Email = accountEntity.EMAIL,
                    PasswordHash = accountEntity.PASSWORD,
                    IsEmailVerified = accountEntity.ISEMAILVERIFIED,
                    CreatedAtUtc = accountEntity.CREATEDATUTC,
                    UpdatedAtUtc = accountEntity.UPDATEDATUTC,
                    LastLoginUtc = accountEntity.LASTLOGINUTC
                };

                profile = new UserProfileDto
                {
                    UserId = profileEntity.USERID,
                    DisplayName = profileEntity.DISPLAYNAME,
                    IsActive = profileEntity.ISACTIVE,
                    CreatedAtUtc = profileEntity.CREATEDATUTC
                };

                return true;
            }
        }

        public void UpdateLastLoginUtc(LoginAccountArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var account = dataBaseContext.ACCOUNT
                    .Single(a => a.EMAIL == args.Email);

                account.LASTLOGINUTC = args.LastLoginUtcDate;
                dataBaseContext.SaveChanges();
            }
        }

        public (AccountDto account, UserProfileDto profile) UpdateDisplayNameAndPassword(UpdateAccountArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.USERID == args.UserId) ?? throw new InvalidOperationException("Account not found.");

                var userProfileEntity = dataBaseContext.USER_PROFILE
                    .SingleOrDefault(p => p.USERID == accountEntity.USERID) ?? throw new InvalidOperationException("User profile not found.");

                userProfileEntity.DISPLAYNAME = args.NewDisplayName;
                accountEntity.PASSWORD = args.NewPassword;
                accountEntity.UPDATEDATUTC = args.UpdatedAtUtc;

                dataBaseContext.SaveChanges();
                transaction.Commit();

                var accountDto = new AccountDto
                {
                    AccountId = accountEntity.ACCOUNTID,
                    UserId = accountEntity.USERID,
                    Email = accountEntity.EMAIL,
                    PasswordHash = accountEntity.PASSWORD,
                    IsEmailVerified = accountEntity.ISEMAILVERIFIED,
                    CreatedAtUtc = accountEntity.CREATEDATUTC,
                    UpdatedAtUtc = accountEntity.UPDATEDATUTC,
                    LastLoginUtc = accountEntity.LASTLOGINUTC
                };

                var profileDto = new UserProfileDto
                {
                    UserId = userProfileEntity.USERID,
                    DisplayName = userProfileEntity.DISPLAYNAME,
                    IsActive = userProfileEntity.ISACTIVE,
                    CreatedAtUtc = userProfileEntity.CREATEDATUTC
                };

                return (accountDto, profileDto);
            }
        }

        public bool GetAccountWithProfileByIdAccount(long userId, out AccountDto account, out UserProfileDto profile)
        {
            account = null;
            profile = null;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.USERID == userId);

                if (accountEntity == null)
                {
                    return false;
                }

                var profileEntity = dataBaseContext.USER_PROFILE
                    .SingleOrDefault(a => a.USERID == accountEntity.USERID);

                if (profileEntity == null)
                {
                    return false;
                }

                account = new AccountDto
                {
                    AccountId = accountEntity.ACCOUNTID,
                    UserId = accountEntity.USERID,
                    Email = accountEntity.EMAIL,
                    PasswordHash = accountEntity.PASSWORD,
                    IsEmailVerified = accountEntity.ISEMAILVERIFIED,
                    CreatedAtUtc = accountEntity.CREATEDATUTC,
                    UpdatedAtUtc = accountEntity.UPDATEDATUTC,
                    LastLoginUtc = accountEntity.LASTLOGINUTC
                };

                profile = new UserProfileDto
                {
                    UserId = profileEntity.USERID,
                    DisplayName = profileEntity.DISPLAYNAME,
                    IsActive = profileEntity.ISACTIVE,
                    CreatedAtUtc = profileEntity.CREATEDATUTC
                };

                return true;
            }
        }
    }
}
