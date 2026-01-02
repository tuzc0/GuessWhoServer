using GuessWhoServerDomain.Domain.Models.EmailVerification;
using System;

namespace GuessWhoDataAccess.Data.DataAccess.EmailVerification
{
    internal static class EmailVerificationTokenRecordMapper
    {
        internal static EmailVerificationTokenRecord ToRecord(EMAIL_VERIFICATION entity)
        {
            if (entity == null)
            {
                return EmailVerificationTokenRecord.CreateInvalid();
            }

            return new EmailVerificationTokenRecord
            {
                TokenId = entity.TOKENID,
                AccountId = entity.ACCOUNTID,
                CodeHash = entity.CODEHASH ?? Array.Empty<byte>(),
                CreatedAtUtc = entity.CREATEDATUTC,
                ExpiresUtc = entity.EXPIRESUTC,
                ConsumedUtc = entity.CONSUMEDUTC,
                FailedAttempts = entity.FAILEDATTEMPTS
            };
        }
    }
}
