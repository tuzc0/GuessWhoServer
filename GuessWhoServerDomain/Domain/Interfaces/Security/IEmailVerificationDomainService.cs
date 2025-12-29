using GuessWhoServerDomain.Domain.Parameters.EmailVerification;
using System;

namespace GuessWhoServerDomain.Domain.Interfaces.Security
{
    public interface IEmailVerificationDomainService
    {
        TimeSpan GetVerificationCodeLifetime();

        void ValidateVerificationCodeFormatOrThrow(long accountId, string trimmedCode);

        void ValidateTokenMatchOrThrow(ValidateTokenMatchArgs tokenMatchArgs);

        void ValidateResendLimitsOrThrow(long accountId, DateTime nowUtc);
    }
}
