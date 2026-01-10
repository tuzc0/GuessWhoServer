using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Accounts
{
    public sealed class UserAccountData : IUserAccountRepository
    {
        private const bool DEFAULT_IS_EMAIL_VERIFIED = false;

        private const int INVALID_ACCOUNT = -1;
        private const long MIN_VALID_ENTITY_ID = 1;

        private const int SQL_DUPLICATE_KEY = 2627;
        private const int SQL_DUPLICATE_INDEX = 2601;

        private const string EMPTY = "";

        private readonly GuessWhoDBEntities dataContext;

        public UserAccountData(GuessWhoDBEntities context)
        {
            dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public bool EmailExists(string email)
        {
            string normalizedEmail = NormalizeEmailOrEmpty(email);

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return false;
            }

            return dataContext.ACCOUNT.Any(a => a.EMAIL == normalizedEmail && !a.ISDELETED);
        }

        public CreatedAccountResult CreateAccount(CreateAccountArgs createAccountArgs)
        {
            if (createAccountArgs == null)
            {
                throw new ArgumentNullException(nameof(createAccountArgs));
            }

            string normalizedEmail = NormalizeEmailOrEmpty(createAccountArgs.Email);

            ACCOUNT existingAccount = FindAccountByEmail(normalizedEmail);

            if (existingAccount != null)
            {
                if (!existingAccount.ISDELETED)
                {
                    return CreatedAccountResult.Ok(
                        AccountRecord.CreateInvalid(),
                        UserProfileRecord.CreateInvalid());
                }

                return ReactivateAccount(existingAccount, createAccountArgs);
            }

            return CreateNewAccount(createAccountArgs, normalizedEmail);
        }

        private ACCOUNT FindAccountByEmail(string normalizedEmail)
        {
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return null;
            }

            return dataContext.ACCOUNT.SingleOrDefault(a => a.EMAIL == normalizedEmail);
        }

        private CreatedAccountResult ReactivateAccount(ACCOUNT accountEntity, CreateAccountArgs args)
        {
            USER_PROFILE profileEntity = FindUserProfile(accountEntity.USERID) ?? CreateMissingProfile(accountEntity.USERID, args);

            ApplyReactivationChanges(accountEntity, profileEntity, args);

            dataContext.SaveChanges();

            AccountRecord account = AccountRecordMapper.ToAccountRecord(accountEntity);
            UserProfileRecord profile = AccountRecordMapper.ToUserProfileRecord(profileEntity);

            return CreatedAccountResult.Ok(account, profile);
        }

        private USER_PROFILE CreateMissingProfile(long userId, CreateAccountArgs args)
        {
            var profileEntity = new USER_PROFILE
            {
                USERID = userId,
                DISPLAYNAME = args.DisplayName,
                CREATEDATUTC = args.CreationDate,
                AVATARID = args.AvatarId,
                ISACTIVE = true
            };

            dataContext.USER_PROFILE.Add(profileEntity);

            return profileEntity;
        }

        private static void ApplyReactivationChanges(ACCOUNT accountEntity, USER_PROFILE profileEntity, CreateAccountArgs args)
        {
            DateTime nowUtc = args.CreationDate;

            accountEntity.ISDELETED = false;
            accountEntity.DELETEDATUTC = null;

            accountEntity.PASSWORD = args.PasswordHash ?? Array.Empty<byte>();
            accountEntity.ISEMAILVERIFIED = DEFAULT_IS_EMAIL_VERIFIED;
            accountEntity.UPDATEDATUTC = nowUtc;

            accountEntity.LOCKEDUNTILUTC = null;

            profileEntity.DISPLAYNAME = args.DisplayName;
            profileEntity.AVATARID = args.AvatarId;
            profileEntity.ISACTIVE = true;
        }

        private CreatedAccountResult CreateNewAccount(CreateAccountArgs args, string normalizedEmail)
        {
            var profileEntity = new USER_PROFILE
            {
                DISPLAYNAME = args.DisplayName,
                CREATEDATUTC = args.CreationDate,
                AVATARID = args.AvatarId,
                ISACTIVE = true
            };

            var accountEntity = new ACCOUNT
            {
                EMAIL = normalizedEmail,
                PASSWORD = args.PasswordHash ?? Array.Empty<byte>(),
                ISEMAILVERIFIED = DEFAULT_IS_EMAIL_VERIFIED,
                CREATEDATUTC = args.CreationDate,
                UPDATEDATUTC = args.CreationDate,
                ISDELETED = false,
                DELETEDATUTC = null,
                USER_PROFILE = profileEntity
            };

            dataContext.ACCOUNT.Add(accountEntity);

            try
            {
                dataContext.SaveChanges();
            }
            catch (DbUpdateException ex) when (IsDuplicateEmailDbUpdateException(ex))
            {
                return CreatedAccountResult.Ok(
                    AccountRecord.CreateInvalid(),
                    UserProfileRecord.CreateInvalid());
            }

            AccountRecord account = AccountRecordMapper.ToAccountRecord(accountEntity);
            UserProfileRecord profile = AccountRecordMapper.ToUserProfileRecord(profileEntity);

            return CreatedAccountResult.Ok(account, profile);
        }

        private static bool IsDuplicateEmailDbUpdateException(DbUpdateException ex)
        {
            SqlException sqlEx = ex?.InnerException?.InnerException as SqlException;

            if (sqlEx == null)
            {
                return false;
            }

            return sqlEx.Number == SQL_DUPLICATE_KEY || sqlEx.Number == SQL_DUPLICATE_INDEX;
        }

        public AccountRecord GetAccountByIdAccount(long accountId)
        {
            ACCOUNT accountEntity = dataContext.ACCOUNT.SingleOrDefault(a => a.ACCOUNTID == accountId && !a.ISDELETED);
            return AccountRecordMapper.ToAccountRecord(accountEntity);
        }

        public AccountWithProfileResult GetAccountWithProfileByUserId(long userId)
        {
            ACCOUNT accountEntity = dataContext.ACCOUNT.SingleOrDefault(a => a.USERID == userId && !a.ISDELETED);

            if (accountEntity == null)
            {
                return AccountWithProfileResult.NotFound();
            }

            USER_PROFILE profileEntity = FindUserProfile(accountEntity.USERID);

            if (profileEntity == null)
            {
                return AccountWithProfileResult.NotFound();
            }

            return MapFoundAccountWithProfile(accountEntity, profileEntity);
        }

        public bool MarkEmailVerified(long accountId, DateTime nowUtc)
        {
            ACCOUNT accountEntity = dataContext.ACCOUNT.SingleOrDefault(a => a.ACCOUNTID == accountId && !a.ISDELETED);

            if (accountEntity == null)
            {
                return false;
            }

            if (accountEntity.ISEMAILVERIFIED)
            {
                accountEntity.UPDATEDATUTC = nowUtc;
                return true;
            }

            accountEntity.ISEMAILVERIFIED = true;
            accountEntity.UPDATEDATUTC = nowUtc;

            return true;
        }

        public long GetAccountIdByEmail(string email)
        {
            string normalizedEmail = NormalizeEmailOrEmpty(email);

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return INVALID_ACCOUNT;
            }

            ACCOUNT accountEntity = dataContext.ACCOUNT.FirstOrDefault(a => a.EMAIL == normalizedEmail && !a.ISDELETED);

            return accountEntity != null ? accountEntity.ACCOUNTID : INVALID_ACCOUNT;
        }

        public bool UpdatePasswordOnly(UpdatePasswordArgs passwordUpdateArgs)
        {
            if (passwordUpdateArgs == null)
            {
                throw new ArgumentNullException(nameof(passwordUpdateArgs));
            }

            ACCOUNT accountEntity = dataContext.ACCOUNT.SingleOrDefault(
                a => a.ACCOUNTID == passwordUpdateArgs.AccountId && !a.ISDELETED);

            if (accountEntity == null)
            {
                return false;
            }

            accountEntity.PASSWORD = passwordUpdateArgs.NewPasswordHash ?? Array.Empty<byte>();
            accountEntity.UPDATEDATUTC = passwordUpdateArgs.UpdatedAtUtc;

            return true;
        }

        public UpdatedAccountResult UpdateDisplayNameAndPassword(UpdateAccountArgs updateAccountArgs)
        {
            if (updateAccountArgs == null)
            {
                throw new ArgumentNullException(nameof(updateAccountArgs));
            }

            ACCOUNT accountEntity = dataContext.ACCOUNT.SingleOrDefault(
                a => a.ACCOUNTID == updateAccountArgs.AccountId && !a.ISDELETED);

            if (accountEntity == null)
            {
                return UpdatedAccountResult.Fail();
            }

            USER_PROFILE userProfileEntity = FindUserProfile(accountEntity.USERID);

            if (userProfileEntity == null)
            {
                return UpdatedAccountResult.Fail();
            }

            userProfileEntity.DISPLAYNAME = updateAccountArgs.NewDisplayName;
            userProfileEntity.AVATARID = updateAccountArgs.NewAvatarId;

            if (HasNewPassword(updateAccountArgs.NewPasswordHash))
            {
                accountEntity.PASSWORD = updateAccountArgs.NewPasswordHash;
            }

            accountEntity.UPDATEDATUTC = updateAccountArgs.UpdatedAtUtc;

            AccountRecord account = AccountRecordMapper.ToAccountRecord(accountEntity);
            UserProfileRecord profile = AccountRecordMapper.ToUserProfileRecord(userProfileEntity);

            return UpdatedAccountResult.Ok(account, profile);
        }

        public AccountProfileRecordResult GetAccountWithProfileForLogin(AccountSearchParameters accountSearchParameters, DateTime nowUtc)
        {
            if (accountSearchParameters == null)
            {
                throw new ArgumentNullException(nameof(accountSearchParameters));
            }

            ACCOUNT accountEntity = FindAccountForLogin(accountSearchParameters);

            if (accountEntity == null || accountEntity.ISDELETED)
            {
                return AccountProfileRecordResult.Fail(AccountProfileStatus.NotFoundOrDeleted);
            }

            if (IsAccountLocked(accountEntity, nowUtc))
            {
                return AccountProfileRecordResult.Fail(AccountProfileStatus.Locked);
            }

            USER_PROFILE profileEntity = FindUserProfile(accountEntity.USERID);

            if (profileEntity == null)
            {
                return AccountProfileRecordResult.Fail(AccountProfileStatus.ProfileNotFound);
            }

            AccountRecord account = AccountRecordMapper.ToAccountRecord(accountEntity);
            UserProfileRecord profile = AccountRecordMapper.ToUserProfileRecord(profileEntity);

            return AccountProfileRecordResult.Ok(account, profile);
        }

        public AccountWithProfileResult TryGetAccountWithProfileForUpdate(AccountSearchParameters accountSearchParameters)
        {
            if (accountSearchParameters == null)
            {
                throw new ArgumentNullException(nameof(accountSearchParameters));
            }

            ACCOUNT accountEntity = FindAccountForUpdate(accountSearchParameters);

            if (!IsAccountActive(accountEntity))
            {
                return AccountWithProfileResult.NotFound();
            }

            USER_PROFILE profileEntity = FindUserProfile(accountEntity.USERID);

            if (!IsProfileValidForUpdate(profileEntity))
            {
                return AccountWithProfileResult.NotFound();
            }

            return MapFoundAccountWithProfile(accountEntity, profileEntity);
        }

        public bool UpdateLastLoginUtc(AccountSearchParameters accountSearchParameters, DateTime nowUtc)
        {
            if (accountSearchParameters == null)
            {
                throw new ArgumentNullException(nameof(accountSearchParameters));
            }

            string normalizedEmail = NormalizeEmailOrEmpty(accountSearchParameters.Email);

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return false;
            }

            ACCOUNT accountEntity = dataContext.ACCOUNT.SingleOrDefault(a => a.EMAIL == normalizedEmail && !a.ISDELETED);

            if (accountEntity == null)
            {
                return false;
            }

            accountEntity.LASTLOGINUTC = nowUtc;
            accountEntity.UPDATEDATUTC = nowUtc;

            return true;
        }

        public bool DeleteAccount(long userId, DateTime nowUtc)
        {
            ACCOUNT accountEntity = dataContext.ACCOUNT.SingleOrDefault(a => a.USERID == userId && !a.ISDELETED);

            if (accountEntity == null)
            {
                return false;
            }

            accountEntity.ISDELETED = true;
            accountEntity.DELETEDATUTC = nowUtc;
            accountEntity.UPDATEDATUTC = nowUtc;

            return true;
        }

        public bool MarkUserProfileActive(long userId)
        {
            USER_PROFILE profileEntity = FindUserProfile(userId);

            if (profileEntity == null)
            {
                return false;
            }

            if (profileEntity.ISACTIVE)
            {
                return true;
            }

            profileEntity.ISACTIVE = true;

            return true;
        }

        public bool MarkUserProfileInactive(long userId)
        {
            USER_PROFILE profileEntity = FindUserProfile(userId);

            if (profileEntity == null)
            {
                return false;
            }

            if (!profileEntity.ISACTIVE)
            {
                return true;
            }

            profileEntity.ISACTIVE = false;

            return true;
        }

        private ACCOUNT FindAccountForLogin(AccountSearchParameters accountSearchArgs)
        {
            string normalizedEmail = NormalizeEmailOrEmpty(accountSearchArgs.Email);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return dataContext.ACCOUNT.SingleOrDefault(a => a.EMAIL == normalizedEmail);
            }

            if (accountSearchArgs.UserId >= MIN_VALID_ENTITY_ID)
            {
                return dataContext.ACCOUNT.SingleOrDefault(a => a.USERID == accountSearchArgs.UserId);
            }

            return null;
        }

        private ACCOUNT FindAccountForUpdate(AccountSearchParameters accountSearchArgs)
        {
            string normalizedEmail = NormalizeEmailOrEmpty(accountSearchArgs.Email);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return dataContext.ACCOUNT.SingleOrDefault(a => a.EMAIL == normalizedEmail && !a.ISDELETED);
            }

            if (accountSearchArgs.UserId >= MIN_VALID_ENTITY_ID)
            {
                return dataContext.ACCOUNT.SingleOrDefault(a => a.USERID == accountSearchArgs.UserId && !a.ISDELETED);
            }

            return null;
        }

        private USER_PROFILE FindUserProfile(long userId)
        {
            return dataContext.USER_PROFILE.SingleOrDefault(p => p.USERID == userId);
        }

        private static bool IsAccountActive(ACCOUNT accountEntity)
        {
            return accountEntity != null && !accountEntity.ISDELETED;
        }

        private static bool IsAccountLocked(ACCOUNT accountEntity, DateTime nowUtc)
        {
            return accountEntity.LOCKEDUNTILUTC.HasValue && accountEntity.LOCKEDUNTILUTC.Value > nowUtc;
        }

        private static bool IsProfileValidForUpdate(USER_PROFILE profileEntity)
        {
            return profileEntity != null && profileEntity.ISACTIVE;
        }

        private static bool HasNewPassword(byte[] newPasswordHash)
        {
            return newPasswordHash != null && newPasswordHash.Length > 0;
        }

        private static string NormalizeEmailOrEmpty(string email)
        {
            return (email ?? EMPTY).Trim().ToLowerInvariant();
        }

        private static AccountWithProfileResult MapFoundAccountWithProfile(ACCOUNT accountEntity, USER_PROFILE profileEntity)
        {
            AccountRecord account = AccountRecordMapper.ToAccountRecord(accountEntity);
            UserProfileRecord profile = AccountRecordMapper.ToUserProfileRecord(profileEntity);

            return AccountWithProfileResult.Found(account, profile);
        }
    }
}
