using GuessWhoServerDomain.Domain.Models.EmailVerification;
using System;

namespace GuessWhoServerDomain.Domain.Parameters.EmailVerification
{
    public sealed class ValidateTokenMatchArgs
    {
        public ValidateTokenMatchArgs(
            long accountId,
            string trimmedCode,
            EmailVerificationTokenRecord token,
            DateTime nowUtc)
        {
            AccountId = accountId;
            TrimmedCode = trimmedCode ?? string.Empty;
            Token = token;
            NowUtc = nowUtc;
        }

        public long AccountId { get; }
        public string TrimmedCode { get; }
        public EmailVerificationTokenRecord Token { get; }
        public DateTime NowUtc { get; }
    }
}
