using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Data;
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
        public RegisterResponse RegisterUser(RegisterRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "Registration request cannot be null.");
            }

            var email = (request.Email ?? "").Trim().ToLowerInvariant();
            var displayName = (request.DisplayName ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;
            var nowUtc = DateTime.UtcNow;

            string verificationCode;

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

                    var (Plain, Hash) = CreateVerificationCodeOrFault();
                    verificationCode = Plain;
                    var verificationCodeHash = Hash;


                    var token = new EMAIL_VERIFICATION
                    {
                        TOKENID = Guid.NewGuid(),
                        ACCOUNTID = account.ACCOUNTID,
                        CODEHASH = verificationCodeHash,
                        EXPIRESUTC = nowUtc.AddMinutes(10),
                        CREATEDATUTC = nowUtc,
                        CONSUMEDUTC = null
                    };

                    dataBaseContext.EMAIL_VERIFICATION.Add(token);
                    dataBaseContext.SaveChanges();
                    transaction.Commit();

                    TrySendVerificationEmailOrThrow(account.EMAIL, verificationCode);

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
                catch (DbUpdateException ex) when (SqlExceptionInspector.IsUniqueViolation(ex, "UQ_ACCOUNT_EMAIL"))
                {
                    transaction.Rollback();

                    throw Faults.Create("DuplicateEmail", "Email already registered.");
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

        public VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request)
        {
            var currentUtcTimestamp = DateTime.UtcNow;
            var normalizedCode = (request.Code ?? string.Empty).Trim();

            if (!Regex.IsMatch(normalizedCode, @"^\d{6}$"))
            {
                throw Faults.Create("InvalidOrExpiredCode", "Invalid code.");
            }

            using (var dataBaseContext = new GuessWhoDB())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == request.AccountId)
                    ?? throw Faults.Create("NotFound", "Account not found.");

                if (accountEntity.ISEMAILVERIFIED)
                {
                    return new VerifyEmailResponse { Success = true };
                }

                var activeVerificationToken = dataBaseContext.EMAIL_VERIFICATION
                    .Where(t => t.ACCOUNTID == request.AccountId &&
                                t.CONSUMEDUTC == null &&
                                t.EXPIRESUTC >= currentUtcTimestamp)
                    .OrderByDescending(t => t.CREATEDATUTC)
                    .FirstOrDefault() ?? throw Faults.Create("InvalidOrExpiredCode", "Code expired.");

                var enteredVerificationCodeHash = VerificationCodeGenerator.ComputeSha256Hash(normalizedCode);

                if (!AreByteSequencesEqualInConstantTime(enteredVerificationCodeHash, activeVerificationToken.CODEHASH))
                {
                    dataBaseContext.Database.ExecuteSqlCommand(
                        @"UPDATE dbo.EMAIL_VERIFICATION
                        SET FAILEDATTEMPTS = FAILEDATTEMPTS + 1,
                        EXPIRESUTC = CASE WHEN FAILEDATTEMPTS + 1 >= 5 THEN @p0 ELSE EXPIRESUTC END
                        WHERE TOKENID = @p1 AND CONSUMEDUTC IS NULL AND EXPIRESUTC >= @p0",
                        currentUtcTimestamp, activeVerificationToken.TOKENID);

                    throw Faults.Create("InvalidOrExpiredCode", "Invalid or expired code.");
                }

                var affectedRows = dataBaseContext.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.EMAIL_VERIFICATION
                    SET CONSUMEDUTC = SYSUTCDATETIME()
                    WHERE TOKENID = @p0 AND CONSUMEDUTC IS NULL",
                    activeVerificationToken.TOKENID);

                if (affectedRows == 0)
                {
                    throw Faults.Create("InvalidOrExpiredCode", "Invalid or expired code.");
                }

                accountEntity.ISEMAILVERIFIED = true;
                accountEntity.UPDATEDATUTC = currentUtcTimestamp;

                dataBaseContext.SaveChanges();
                transaction.Commit();

                return new VerifyEmailResponse { Success = true };
            }
        }


        public void ResendEmailVerificationCode(ResendVerificationRequest request)
        {
            var currentUtcTimestamp = DateTime.UtcNow;

            using (var dataBaseContext = new GuessWhoDB())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var accountEntity = dataBaseContext.ACCOUNT
                    .SingleOrDefault(a => a.ACCOUNTID == request.AccountId)
                    ?? throw Faults.Create("NotFound", "Account not found.");

                if (accountEntity.ISEMAILVERIFIED)
                {
                    return;
                }

                var lastVerificationToken = dataBaseContext.EMAIL_VERIFICATION
                    .Where(t => t.ACCOUNTID == request.AccountId)
                    .OrderByDescending(t => t.CREATEDATUTC)
                    .FirstOrDefault();

                if (lastVerificationToken != null &&
                    (currentUtcTimestamp - lastVerificationToken.CREATEDATUTC).TotalSeconds < 60)
                {
                    throw Faults.Create("RateLimited", "Try again in one minute.");
                }

                var oneHourAgo = currentUtcTimestamp.AddHours(-1);

                var tokensSentInLastHour = dataBaseContext.EMAIL_VERIFICATION
                    .Count(t => t.ACCOUNTID == request.AccountId &&
                                t.CREATEDATUTC >= oneHourAgo);

                if (tokensSentInLastHour >= 5)
                {
                    throw Faults.Create("RateLimited", "Resend limit reached.");
                }

                dataBaseContext.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.EMAIL_VERIFICATION
                    SET EXPIRESUTC = @p0
                    WHERE ACCOUNTID = @p1
                    AND CONSUMEDUTC IS NULL
                    AND EXPIRESUTC > @p0",
                    currentUtcTimestamp, accountEntity.ACCOUNTID);

                var (Plain, Hash) = CreateVerificationCodeOrFault();
                var verificationCode = Plain;
                var verificationCodeHash = Hash;

                var newVerificationToken = new EMAIL_VERIFICATION
                {
                    TOKENID = Guid.NewGuid(),
                    ACCOUNTID = accountEntity.ACCOUNTID,
                    CODEHASH = verificationCodeHash,
                    EXPIRESUTC = currentUtcTimestamp.AddMinutes(10),
                    CREATEDATUTC = currentUtcTimestamp
                };

                dataBaseContext.EMAIL_VERIFICATION.Add(newVerificationToken);
                dataBaseContext.SaveChanges();

                transaction.Commit();

                TrySendVerificationEmailOrThrow(accountEntity.EMAIL, verificationCode);
            }
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
            catch (Exception)
            {
                throw Faults.Create("EmailSendFailed", "Unable to send verification email.");
            }
        }

        private static (string Plain, byte[] Hash) CreateVerificationCodeOrFault()
        {
            try
            {
                var code = VerificationCodeGenerator.GenerateNumericCode();
                var hashCode = VerificationCodeGenerator.ComputeSha256Hash(code);
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
