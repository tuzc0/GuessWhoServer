using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using ClassLibraryGuessWho.Data.Helpers; 
using GuessWho.Contracts.Dtos;
using GuessWho.Contracts.Services;
using GuessWho.Services.Security;
using GuessWho.Services.WCF.Security;
using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net.Mail;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text.RegularExpressions;
using WcfServiceLibraryGuessWho.Communication.Email;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false)]
    public class UserService : IUserService
    {
        private readonly TimeSpan VerificationCodeLifeTime = TimeSpan.FromMinutes(10);
        private readonly UserAccountData userAccountData = new UserAccountData();
        private readonly EmailVerificationData emailVerificationData = new EmailVerificationData();

        public RegisterResponse RegisterUser(RegisterRequest request)
        {

            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "Registration request cannot be null.");
            }

            var email = (request.Email ?? "").Trim().ToLowerInvariant();
            var displayName = (request.DisplayName ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;
            var dateNowUtc = DateTime.UtcNow;

            try
            {

                if (userAccountData.EmailExists(email))
                {
                    throw Faults.Create("DuplicateEmail", "Email already registered.");
                }

                var passwordHash = PasswordHasher.HashPassword(password);
                var (Plain, Hash) = CreateVerificationCodeOrFault();

                var createAccountArgs = new CreateAccountArgs
                {
                    Email = email,
                    Password = passwordHash,
                    DisplayName = displayName,
                    CreationDate = dateNowUtc
                };

                var (account, profile) = userAccountData.CreateAccount(createAccountArgs);

                var createTokenArgs = new CreateEmailTokenArgs
                {
                    AccountId = account.ACCOUNTID,
                    CodeHash = Hash,
                    NowUtc = dateNowUtc,
                    LifeSpan = VerificationCodeLifeTime
                };

                emailVerificationData.AddVerificationToken(createTokenArgs);

                TrySendVerificationEmailOrThrow(account.EMAIL, Plain);

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
                throw;
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsUniqueViolation(ex, "UQ_ACCOUNT_EMAIL"))
            {
                throw Faults.Create("DuplicateEmail", "Email already registered.");
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsForeignKeyViolation(ex))
            {
                throw Faults.Create("ForeignKey", "Operation violates an existing relation.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                throw Faults.Create("DatabaseTimeout", "The database did not respond in time.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                throw Faults.Create("DatabaseConnection", "Unable to connect to the database.");
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                System.Diagnostics.Trace.TraceError($"[UserService.RegisterUser] {ex}");
                throw Faults.Create("Unexpected", "Unexpected server error.");
            }
        }

        public VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request)
        {

            var currentUtcTimestamp = DateTime.UtcNow;
            var code = (request.Code ?? string.Empty).Trim();

            if (!Regex.IsMatch(code, @"^\d{6}$"))
            {
                throw Faults.Create("InvalidOrExpiredCode", "Invalid code.");
            }

            var account = userAccountData.GetAccountById(request.AccountId)
                ?? throw Faults.Create("NotFound", "Account not found.");

            if (account.ISEMAILVERIFIED)
            {
                return new VerifyEmailResponse { Success = true };
            }

            var token = emailVerificationData.GetLatestTokenByAccountId(request.AccountId, currentUtcTimestamp)
                ?? throw Faults.Create("InvalidOrExpiredCode", "Invalid or expired code.");

            var codeHash = CodeGenerator.ComputeSha256Hash(code);

            if (!AreByteSequencesEqualInConstantTime(codeHash, token.CODEHASH))
            {
                emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(token.TOKENID, currentUtcTimestamp);
                throw Faults.Create("InvalidOrExpiredCode", "Invalid or expired code.");
            }

            var consumedRows = emailVerificationData.ConsumeToken(token.TOKENID);

            if (consumedRows == 0)
            {
                throw Faults.Create("InvalidOrExpiredCode", "Invalid or expired code.");
            }

            userAccountData.MarkEmailVerified(request.AccountId, currentUtcTimestamp);
            return new VerifyEmailResponse { Success = true };
        }


        public void ResendEmailVerificationCode(ResendVerificationRequest request)
        {
            var currentUtcTimestamp = DateTime.UtcNow;

            var account = userAccountData.GetAccountById(request.AccountId)
                ?? throw Faults.Create("NotFound", "Account not found.");

            if (account.ISEMAILVERIFIED)
            {
                return; 
            }

            var (perMinute, withingHourcap, _, _) = emailVerificationData.GetEmailVerificationResendLimits(
                request.AccountId, currentUtcTimestamp);

            if (perMinute)
            {
                throw Faults.Create("RateLimited", "Try again in one minute.");
            }

            if (!withingHourcap)
            {
                throw Faults.Create("HourlyLimitExceeded", "Hourly resend limit exceeded.");
            }

            var (Plain, Hash) = CreateVerificationCodeOrFault();
            var createTokenArgs = new CreateEmailTokenArgs
            {
                AccountId = request.AccountId,
                CodeHash = Hash,
                NowUtc = currentUtcTimestamp,
                LifeSpan = VerificationCodeLifeTime
            };

            emailVerificationData.AddVerificationToken(createTokenArgs);
            TrySendVerificationEmailOrThrow(account.EMAIL,Plain);
        }

        private static bool AreByteSequencesEqualInConstantTime(byte[] firstByteSequence, byte[] secondByteSequence)
        {

            if (firstByteSequence == null || secondByteSequence == null)
            {
                return firstByteSequence == secondByteSequence;
            }

            int accumulatedDifference = firstByteSequence.Length ^ secondByteSequence.Length;
            int maxLength = Math.Max(firstByteSequence.Length, secondByteSequence.Length);

            for (int byteIndex = 0; byteIndex < maxLength; byteIndex++)
            {
                byte firstByte = byteIndex < firstByteSequence.Length ? firstByteSequence[byteIndex] : (byte)0;
                byte secondByte = byteIndex < secondByteSequence.Length ? secondByteSequence[byteIndex] : (byte)0;
                accumulatedDifference |= firstByte ^ secondByte;
            }

            return accumulatedDifference == 0;
        }

        private static void TrySendVerificationEmailOrThrow(string email, string code)
        {

            try
            {
                new VerificationEmailSender().SendVerificationCode(email, code);
            }
            catch (EmailSendException ex)
            {
                throw Faults.Create(ex.Code, ex.Message);
            }
            catch (ArgumentException ex) when (ex.ParamName == "recipientEmailAddress")
            {
                throw Faults.Create("EmailInvalid", "Invalid recipient email.");
            }
            catch (ArgumentException ex) when (ex.ParamName == "verificationCode")
            {
                throw Faults.Create("VerificationCodeInvalid", "Verification code must be 6 digits.");
            }
            catch (InvalidOperationException)
            {
                throw Faults.Create("SMTPNotConfigured", "SMTP settings are missing.");
            }
            catch (AuthenticationException)
            {
                throw Faults.Create("SMTPAuthenticationFailed", "SMTP authentication failed.");
            }
            catch (SmtpException ex) when (
                   ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst ||
                   ex.StatusCode == SmtpStatusCode.ClientNotPermitted ||
                   ex.StatusCode == SmtpStatusCode.CommandNotImplemented)
            {
                throw Faults.Create("SMTPConfigurationError", "SMTP configuration error.");
            }
            catch (SmtpException ex) when (
                   ex.StatusCode == SmtpStatusCode.GeneralFailure ||
                   ex.StatusCode == SmtpStatusCode.TransactionFailed ||
                   ex.StatusCode == SmtpStatusCode.MailboxBusy ||
                   ex.StatusCode == SmtpStatusCode.InsufficientStorage)
            {
                throw Faults.Create("SMTPUnavailable", "Email service unavailable.");
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                System.Diagnostics.Trace.TraceError($"[UserService.RegisterUser] {ex}");
                throw Faults.Create("EmailSendFailed", "Unable to send verification email.");
            }
        }

        private static (string Plain, byte[] Hash) CreateVerificationCodeOrFault()
        {

            try
            {
                var code = CodeGenerator.GenerateNumericCode();
                var hashCode = CodeGenerator.ComputeSha256Hash(code);
                return (code, hashCode);
            }
            catch (ArgumentNullException)
            {
                throw Faults.Create("CryptoUnavailable", "Secure random generator is unavailable.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw Faults.Create("VerificationCodeGenerationFailed", "Unable to generate verification code.");
            }
            catch (CryptographicException)
            {
                throw Faults.Create("CryptoUnavailable", "Secure random generator is unavailable.");
            }
        }
    }
}
