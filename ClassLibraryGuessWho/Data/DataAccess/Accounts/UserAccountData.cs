using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using System;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts
{
    public sealed class UserAccountData : IUserAccountData
    {
        private readonly GuessWhoDBEntities dataContext; 

        public UserAccountData(GuessWhoDBEntities context)
        {
            this.dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public bool EmailExists(string email)
        {
            bool emailExists = false;
          
            emailExists = dataContext.ACCOUNT.Any(ua => ua.EMAIL == email);
      
            return emailExists;
        }

        public (AccountDto account, UserProfileDto profile) CreateAccount(CreateAccountArgs args)
        {
            using (var transaction = dataContext.Database.BeginTransaction())
            {
                var profileEntity = new USER_PROFILE
                {
                    DISPLAYNAME = args.DisplayName,
                    ISACTIVE = true,
                    CREATEDATUTC = args.CreationDate
                };

                dataContext.USER_PROFILE.Add(profileEntity);
                dataContext.SaveChanges();

                var accountEntity = new ACCOUNT
                {
                    USERID = profileEntity.USERID,
                    EMAIL = args.Email,
                    PASSWORD = args.Password,
                    ISEMAILVERIFIED = false,
                    CREATEDATUTC = args.CreationDate,
                    UPDATEDATUTC = args.CreationDate
                };

                dataContext.ACCOUNT.Add(accountEntity);
                dataContext.SaveChanges();

                transaction.Commit();

                var accountDto = ToAccountDto(accountEntity);
                var profileDto = ToUserProfileDto(profileEntity);

                return (accountDto, profileDto);
            }
        }

        public AccountDto GetAccountByIdAccount(long accountId)
        {
            var accountEntity = dataContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == accountId);

            if (accountEntity == null)
            {
                return AccountDto.CreateInvalid();
            }

            return ToAccountDto(accountEntity);
        }

        public bool MarkEmailVerified(long accountId, DateTime nowUtc)
        {
            bool isUpdated = false;

            var accountEntity = dataContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == accountId);

            if (accountEntity != null)
            {
                accountEntity.ISEMAILVERIFIED = true;
                accountEntity.UPDATEDATUTC = nowUtc;

                dataContext.SaveChanges();
                isUpdated = true;
            }

            return isUpdated;
        }

        public bool MarkUserProfileActive(long userId)
        {
            var profileEntity = dataContext.USER_PROFILE
                    .SingleOrDefault(p => p.USERID == userId);

            if (profileEntity == null)
            {
                return false;
            }

            if (profileEntity.ISACTIVE)
            {
                return true;
            }

            profileEntity.ISACTIVE = true;

            return dataContext.SaveChanges() > 0;
        }

        public bool MarkUserProfileInactive(long userId)
        {
            var profileEntity = dataContext.USER_PROFILE
                    .SingleOrDefault(p => p.USERID == userId);

            if (profileEntity == null)
            {
                return false;
            }

            if (!profileEntity.ISACTIVE)
            {
                return true;
            }

            profileEntity.ISACTIVE = false;

            dataContext.SaveChanges();
            return true;
        }

        public AccountProfileResult GetAccountWithProfileForLogin(AccountSearchParameters args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var accountEntity = FindAccountForLogin(dataContext, args);

            if (accountEntity == null || accountEntity.ISDELETED)
            {
                return AccountProfileResult.Fail(AccountProfileStatus.NotFoundOrDeleted);
            }

            if (accountEntity.LOCKEDUNTILUTC.HasValue
                && accountEntity.LOCKEDUNTILUTC.Value > DateTime.UtcNow)
            {
                return AccountProfileResult.Fail(AccountProfileStatus.Locked);
            }

            var profileEntity = FindUserProfile(dataContext, accountEntity.USERID);

            if (profileEntity == null)
            {
                return AccountProfileResult.Fail(AccountProfileStatus.ProfileNotFound);
            }

            if (profileEntity.ISACTIVE)
            {
                return AccountProfileResult.Fail(AccountProfileStatus.ProfileAlreadyActive);
            }

            var account = ToAccountDto(accountEntity);
            var profile = ToUserProfileDto(profileEntity);

            return AccountProfileResult.Ok(account, profile);
        }

        public (AccountDto account, UserProfileDto profile) 
            TryGetAccountWithProfileForUpdate(AccountSearchParameters args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var accountEntity = FindAccountForUpdate(dataContext, args);

            if (!IsAccountValidForUpdate(accountEntity))
            {
                return CreateInvalidAccountWithProfileResult();
            }

            var profileEntity = FindUserProfile(dataContext, accountEntity.USERID);

            if (!IsProfileValidForUpdate(profileEntity))
            {
                return CreateInvalidAccountWithProfileResult();
            }

            var account = ToAccountDto(accountEntity);
            var profile = ToUserProfileDto(profileEntity);

            return (account, profile);
        }

        public bool UpdateLastLoginUtc(AccountSearchParameters args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            bool isUpdated = false;

            var accountEntity = dataContext.ACCOUNT
                    .SingleOrDefault(a => a.EMAIL == args.Email && !a.ISDELETED);

            if (accountEntity != null)
            {
                accountEntity.LASTLOGINUTC = DateTime.UtcNow;

                dataContext.SaveChanges();
                isUpdated = true;
            }

            return isUpdated;
        }

        public bool DeleteAccount(long userId)
        {
            var accountEntity = dataContext.ACCOUNT
                    .SingleOrDefault(a => a.USERID == userId && !a.ISDELETED);

            if (accountEntity == null)
            {
                return false;
            }

            var nowUtc = DateTime.UtcNow;

            accountEntity.ISDELETED = true;
            accountEntity.DELETEDATUTC = nowUtc;
            accountEntity.UPDATEDATUTC = nowUtc;

            dataContext.SaveChanges();
            return true;
        }

        public (AccountDto account, UserProfileDto profile) UpdateDisplayNameAndPassword(UpdateAccountArgs args)
        {
            using (var transaction = dataContext.Database.BeginTransaction())
            {
                var accountEntity = dataContext.ACCOUNT
                    .SingleOrDefault(a => a.USERID == args.UserId)
                    ?? throw new InvalidOperationException("Account not found.");

                var userProfileEntity = dataContext.USER_PROFILE
                    .SingleOrDefault(p => p.USERID == accountEntity.USERID)
                    ?? throw new InvalidOperationException("User profile not found.");

                userProfileEntity.DISPLAYNAME = args.NewDisplayName;
                userProfileEntity.AVATARID = args.NewAvatarId;
                accountEntity.PASSWORD = args.NewPassword;
                accountEntity.UPDATEDATUTC = args.UpdatedAtUtc;

                dataContext.SaveChanges();
                transaction.Commit();

                var accountDto = ToAccountDto(accountEntity);
                var profileDto = ToUserProfileDto(userProfileEntity);

                return (accountDto, profileDto);
            }
        }

        public (AccountDto account, UserProfileDto profile) GetAccountWithProfileByIdAccount(long userId)
        {
            var accountEntity = dataContext.ACCOUNT
                    .SingleOrDefault(a => a.USERID == userId);

            AccountDto account;
            UserProfileDto profile;

            if (accountEntity == null)
            {
                account = AccountDto.CreateInvalid();
                profile = UserProfileDto.CreateInvalid();
                return (account, profile);
            }

            account = ToAccountDto(accountEntity);

            var profileEntity = dataContext.USER_PROFILE
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

        public long GetAccountIdByEmail(string email)
        {
            var account = dataContext.ACCOUNT
                    .FirstOrDefault(a => a.EMAIL == email && !a.ISDELETED);

            return account != null ? account.ACCOUNTID : 0;
        }

        public bool UpdatePasswordOnly(long accountId, byte[] newPasswordHash)
        {
            var account = dataContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == accountId);

            if (account == null)
            {
                return false;
            }

            account.PASSWORD = newPasswordHash;
            account.UPDATEDATUTC = DateTime.UtcNow;

            dataContext.SaveChanges();
            return true;
        }

        private static (AccountDto account, UserProfileDto profile) CreateInvalidAccountWithProfileResult()
        {
            return (AccountDto.CreateInvalid(), UserProfileDto.CreateInvalid());
        }

        private static ACCOUNT FindAccountForLogin(
            GuessWhoDBEntities context,
            AccountSearchParameters args)
        {
            if (!string.IsNullOrWhiteSpace(args.Email))
            {
                return context.ACCOUNT
                    .SingleOrDefault(a => a.EMAIL == args.Email);
            }

            if (args.UserId > 0)
            {
                return context.ACCOUNT
                    .SingleOrDefault(a => a.USERID == args.UserId);
            }

            return null;
        }

        private static ACCOUNT FindAccountForUpdate(GuessWhoDBEntities context,
            AccountSearchParameters args)
        {
            if (!string.IsNullOrWhiteSpace(args.Email))
            {
                return context.ACCOUNT
                    .SingleOrDefault(a => a.EMAIL == args.Email && !a.ISDELETED);
            }

            if (args.UserId > 0)
            {
                return context.ACCOUNT
                    .SingleOrDefault(a => a.USERID == args.UserId && !a.ISDELETED);
            }

            return null;
        }

        private static bool IsAccountValidForUpdate(ACCOUNT accountEntity)
        {
            if (accountEntity == null)
            {
                return false;
            }

            if (accountEntity.ISDELETED)
            {
                return false;
            }

            return true;
        }

        private static USER_PROFILE FindUserProfile(GuessWhoDBEntities context,
            long userId)
        {
            return context.USER_PROFILE
                .SingleOrDefault(p => p.USERID == userId);
        }

        private static bool IsProfileValidForUpdate(USER_PROFILE profileEntity)
        {
            if (profileEntity == null)
            {
                return false;
            }

            if (!profileEntity.ISACTIVE)
            {
                return false;
            }

            return true;
        }
    }
}
