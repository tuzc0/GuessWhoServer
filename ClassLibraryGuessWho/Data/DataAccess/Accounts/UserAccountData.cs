using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
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
                    CreatedAtUtc = profileEntity.CREATEDATUTC,
                    AvatarId = profileEntity.AVATARID
                };

                return (accountDto, profileDto);
            }
        }

        public AccountDto GetAccountByIdAccount(long accountId)
        {
            AccountDto account;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == accountId);

                if (accountEntity == null)
                {
                    account = AccountDto.CreateInvalid();
                }
                else
                {
                    account = ToAccountDto(accountEntity);
                }

                return account;
            }
        }

        public bool MarkEmailVerified(long accountId, DateTime nowUtc)
        {
            bool isUpdated = false;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == accountId);

                if (accountEntity != null)
                {
                    accountEntity.ISEMAILVERIFIED = true;
                    accountEntity.UPDATEDATUTC = nowUtc;

                    dataBaseContext.SaveChanges();
                    isUpdated = true;
                }
            }

            return isUpdated;
        }

        public (AccountDto account, UserProfileDto profile) TryGetAccountWithProfile(AccountSearchParameters args)
        {
            AccountDto account;
            UserProfileDto profile;

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                ACCOUNT accountEntity = null;

                if (!string.IsNullOrWhiteSpace(args.Email))
                {
                    accountEntity = dataBaseContext.ACCOUNT
                        .SingleOrDefault(a => a.EMAIL == args.Email && !a.ISDELETED);
                }
                else if (args.UserId > 0)
                {
                    accountEntity = dataBaseContext.ACCOUNT
                        .SingleOrDefault(a => a.USERID == args.UserId && !a.ISDELETED);
                }

                var profileEntity = dataBaseContext.USER_PROFILE
                    .SingleOrDefault(p => p.USERID == accountEntity.USERID);

                if (profileEntity == null || accountEntity == null)
                {
                    account = AccountDto.CreateInvalid();
                    profile = UserProfileDto.CreateInvalid();

                } else
                {
                    account = ToAccountDto(accountEntity);
                    profile = ToUserProfileDto(profileEntity);
                }

                return (account, profile);
            }
        }

        public bool UpdateLastLoginUtc(AccountSearchParameters args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            bool isUpdated = false;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.EMAIL == args.Email && !a.ISDELETED);

                if (accountEntity != null)
                {
                    accountEntity.LASTLOGINUTC = DateTime.Now;

                    dataBaseContext.SaveChanges();
                    isUpdated = true;
                }
            }

            return isUpdated;
        }

        public bool DeleteAccount(long userId)
        {
            using (var databaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = databaseContext.ACCOUNT
                    .SingleOrDefault(a => a.USERID == userId && !a.ISDELETED);

                if (accountEntity == null)
                {
                    return false;
                }

                accountEntity.ISDELETED = true;
                accountEntity.DELETEDATUTC = DateTime.UtcNow;
                accountEntity.UPDATEDATUTC = DateTime.UtcNow;

                databaseContext.SaveChanges();
                return true;
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
                userProfileEntity.AVATARID = args.NewAvatarId;
                accountEntity.PASSWORD = args.NewPassword;
                accountEntity.UPDATEDATUTC = args.UpdatedAtUtc;

                dataBaseContext.SaveChanges();
                transaction.Commit();

                var accountDto = ToAccountDto(accountEntity);
                var profileDto = ToUserProfileDto(userProfileEntity);

                return (accountDto, profileDto);
            }
        }

        public (AccountDto account, UserProfileDto profile) GetAccountWithProfileByIdAccount(long userId)
        {
            AccountDto account;
            UserProfileDto profile;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.USERID == userId);

                if (accountEntity == null)
                {
                    account = AccountDto.CreateInvalid();
                }
                else
                {
                    account = ToAccountDto(accountEntity);
                }
                
                var profileEntity = dataBaseContext.USER_PROFILE
                    .SingleOrDefault(a => a.USERID == accountEntity.USERID);

                if (profileEntity == null)
                {
                    profile = UserProfileDto.CreateInvalid();
                }
                else
                {
                    profile = ToUserProfileDto(profileEntity);
                }

                return (account, profile);
            }
        }

        private static AccountDto ToAccountDto(ACCOUNT entity) 
        {
            return new AccountDto
            {
                AccountId = entity.ACCOUNTID,
                UserId = entity.USERID,
                Email = entity.EMAIL,
                PasswordHash = entity.PASSWORD,
                IsEmailVerified = entity.ISEMAILVERIFIED,
                CreatedAtUtc = entity.CREATEDATUTC,
                UpdatedAtUtc = entity.UPDATEDATUTC,
                LastLoginUtc = entity.LASTLOGINUTC
            };
        }

        private static UserProfileDto ToUserProfileDto(USER_PROFILE entity) 
        {
            return new UserProfileDto
            {
                UserId = entity.USERID,
                DisplayName = entity.DISPLAYNAME,
                IsActive = entity.ISACTIVE,
                CreatedAtUtc = entity.CREATEDATUTC,
                AvatarId = entity.AVATARID
            };
        }
    }
}
