using GuessWhoServerDomain.Domain.Models.Accounts;
using System;

namespace GuessWhoDataAccess.Data.DataAccess.Accounts
{
    internal static class AccountRecordMapper
    {
        internal static AccountRecord ToAccountRecord(ACCOUNT entity)
        {
            if (entity == null)
            {
                return AccountRecord.CreateInvalid();
            }

            return new AccountRecord
            {
                AccountId = entity.ACCOUNTID,
                UserId = entity.USERID,
                Email = entity.EMAIL ?? string.Empty,
                PasswordHash = entity.PASSWORD ?? Array.Empty<byte>(),
                IsEmailVerified = entity.ISEMAILVERIFIED,
                LastLoginUtc = entity.LASTLOGINUTC,
                IsDeleted = entity.ISDELETED,
                FailedLogInUtc = entity.FAILEDLOGINS,
                CreatedAtUtc = entity.CREATEDATUTC,
                UpdatedAtUtc = entity.UPDATEDATUTC,
                LockedUntilUtc = entity.LOCKEDUNTILUTC
            };
        }

        internal static UserProfileRecord ToUserProfileRecord(USER_PROFILE entity)
        {
            if (entity == null)
            {
                return UserProfileRecord.CreateInvalid();
            }

            return new UserProfileRecord
            {
                UserId = entity.USERID,
                DisplayName = entity.DISPLAYNAME ?? string.Empty,
                IsActive = entity.ISACTIVE,
                CreatedAtUtc = entity.CREATEDATUTC,
                AvatarId = entity.AVATARID
            };
        }
    }
}
