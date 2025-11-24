using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.DataAccess.Avatars;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWho.Services.Security;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserService));

        private const string FAULT_CODE_REQUEST_NULL = "USER_REQUEST_NULL";
        private const string FAULT_CODE_REGISTER_EMAIL_ALREADY_EXISTS = "USER_REGISTER_EMAIL_ALREADY_EXISTS";
        private const string FAULT_CODE_REGISTER_DATA_INTEGRITY_VIOLATION = "USER_REGISTER_DATA_INTEGRITY_VIOLATION";
        private const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DATABASE_COMMAND_TIMEOUT";
        private const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DATABASE_CONNECTION_FAILURE";
        private const string FAULT_CODE_UNEXPECTED_ERROR = "USER_REGISTER_UNEXPECTED_ERROR";
        private const string FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED = "EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED";
        private const string FAULT_CODE_ACCOUNT_NOT_FOUND = "ACCOUNT_NOT_FOUND";
        private const string FAULT_CODE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT = "EMAIL_VERIFICATION_RESEND_TOO_FREQUENT";
        private const string FAULT_CODE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED = "EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED";
        private const string FAULT_CODE_EMAIL_RECIPIENT_INVALID = "EMAIL_RECIPIENT_INVALID";
        private const string FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT = "EMAIL_VERIFICATION_CODE_INVALID_FORMAT";
        private const string FAULT_CODE_EMAIL_SMTP_CONFIGURATION_MISSING = "EMAIL_SMTP_CONFIGURATION_MISSING";
        private const string FAULT_CODE_EMAIL_SMTP_AUTHENTICATION_FAILED = "EMAIL_SMTP_AUTHENTICATION_FAILED";
        private const string FAULT_CODE_EMAIL_SMTP_CONFIGURATION_ERROR = "EMAIL_SMTP_CONFIGURATION_ERROR";
        private const string FAULT_CODE_EMAIL_SMTP_UNAVAILABLE = "EMAIL_SMTP_UNAVAILABLE";
        private const string FAULT_CODE_EMAIL_SEND_FAILED = "EMAIL_SEND_FAILED";
        private const string FAULT_CODE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE = "CRYPTO_RANDOM_GENERATOR_UNAVAILABLE";
        private const string FAULT_CODE_VERIFICATION_CODE_GENERATION_FAILED = "VERIFICATION_CODE_GENERATION_FAILED";

        private const string FAULT_MESSAGE_REGISTER_REQUEST_NULL =
            "Registration data is missing. Please fill in the form and try again.";
        private const string FAULT_MESSAGE_REGISTER_EMAIL_ALREADY_EXISTS =
            "This email address is already registered. Try signing in or use a different email.";
        private const string FAULT_MESSAGE_REGISTER_DATA_INTEGRITY_VIOLATION =
            "Your account could not be created due to a data consistency issue. Please try again.";
        private const string FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT =
            "The server took too long to respond. Please try again.";
        private const string FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE =
            "The server could not connect to the database. Please try again later.";
        private const string FAULT_MESSAGE_REGISTER_UNEXPECTED_ERROR =
            "An unexpected error occurred while creating your account. Please try again later.";
        private const string FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED =
            "The verification code is invalid or has expired. Please request a new code.";
        private const string FAULT_MESSAGE_ACCOUNT_NOT_FOUND =
            "We could not find an account with the provided information.";
        private const string FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT =
            "You requested a code recently. Please wait a moment and try again.";
        private const string FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED =
            "You have reached the hourly limit for resending verification codes. Please try again later.";
        private const string FAULT_MESSAGE_EMAIL_RECIPIENT_INVALID =
            "The destination email address is not valid. Check it and try again.";
        private const string FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT =
            "The verification code must contain exactly 6 digits.";
        private const string FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_MISSING =
            "The email service is not correctly configured. Please try again later.";
        private const string FAULT_MESSAGE_EMAIL_SMTP_AUTHENTICATION_FAILED =
            "The email service could not authenticate with the server. Please try again later.";
        private const string FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_ERROR =
            "The email service is not available due to a configuration problem. Please try again later.";
        private const string FAULT_MESSAGE_EMAIL_SMTP_UNAVAILABLE =
            "The email service is temporarily unavailable. Please try again later.";
        private const string FAULT_MESSAGE_EMAIL_SEND_FAILED =
            "We could not send the verification email. Please try again later.";
        private const string FAULT_MESSAGE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE =
            "The system could not generate a secure verification code. Please try again later.";
        private const string FAULT_MESSAGE_VERIFICATION_CODE_GENERATION_FAILED =
            "We could not generate a verification code. Please try again.";

        private readonly UserAccountData userAccountData = new UserAccountData();
        private readonly EmailVerificationData emailVerificationData = new EmailVerificationData();
        private readonly AvatarData avatarData = new AvatarData();
        private static readonly TimeSpan VerificationCodeLifeTime = TimeSpan.FromMinutes(10);

        public RegisterResponse RegisterUser(RegisterRequest request)
        {
            if (request == null)
            {
                Logger.Warn("RegisterUser request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REGISTER_REQUEST_NULL);
            }

            string email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            string displayName = (request.DisplayName ?? string.Empty).Trim();
            string password = request.Password ?? string.Empty;
            DateTime dateNowUtc = DateTime.UtcNow;

            try
            {
                if (userAccountData.EmailExists(email))
                {
                    Logger.WarnFormat("RegisterUser failed: email '{0}' is already registered.", email);

                    throw Faults.Create(
                        FAULT_CODE_REGISTER_EMAIL_ALREADY_EXISTS,
                        FAULT_MESSAGE_REGISTER_EMAIL_ALREADY_EXISTS);
                }

                var passwordHash = PasswordHasher.HashPassword(password);
                var verificationCodeResult = CreateVerificationCodeOrFault();
                var avatarDefaultId = avatarData.GetDefaultAvatarId();

                var createAccountArgs = new CreateAccountArgs
                {
                    Email = email,
                    Password = passwordHash,
                    DisplayName = displayName,
                    CreationDate = dateNowUtc,
                    AvatarId = avatarDefaultId
                };

                var (account, profile) = userAccountData.CreateAccount(createAccountArgs);

                var createTokenArgs = new CreateEmailTokenArgs
                {
                    AccountId = account.AccountId,
                    CodeHash = verificationCodeResult.HashCode,
                    NowUtc = dateNowUtc,
                    LifeSpan = VerificationCodeLifeTime
                };

                emailVerificationData.AddVerificationToken(createTokenArgs);

                TrySendVerificationEmailOrThrow(account.Email, verificationCodeResult.PlainCode);

                Logger.InfoFormat("RegisterUser succeeded for email '{0}', accountId '{1}', userId '{2}'.",
                    email, account.AccountId, profile.UserId);

                return new RegisterResponse
                {
                    AccountId = account.AccountId,
                    UserId = profile.UserId,
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
                Logger.Fatal("Email already registered during account creation (unique index violation).", ex);
                throw Faults.Create(
                    FAULT_CODE_REGISTER_EMAIL_ALREADY_EXISTS,
                    FAULT_MESSAGE_REGISTER_EMAIL_ALREADY_EXISTS,
                    ex);
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsForeignKeyViolation(ex))
            {
                Logger.Fatal("Data integrity violation while creating account (foreign key).", ex);
                throw Faults.Create(
                    FAULT_CODE_REGISTER_DATA_INTEGRITY_VIOLATION,
                    FAULT_MESSAGE_REGISTER_DATA_INTEGRITY_VIOLATION,
                    ex);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database command timeout during registration.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                    FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                    ex);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure during registration.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                    FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                    ex);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected error during registration.", ex);
                throw Faults.Create(
                    FAULT_CODE_UNEXPECTED_ERROR,
                    FAULT_MESSAGE_REGISTER_UNEXPECTED_ERROR,
                    ex);
            }
        }

        public VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request)
        {
            DateTime currentUtcTimestamp = DateTime.UtcNow;
            string code = (request.Code ?? string.Empty).Trim();

            Logger.InfoFormat("ConfirmEmailAddressWithVerificationCode attempt for accountId '{0}'.", request.AccountId);

            if (!Regex.IsMatch(code, @"^\d{6}$"))
            {
                Logger.WarnFormat("ConfirmEmailAddressWithVerificationCode failed: invalid code format for accountId '{0}'.", 
                    request.AccountId);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED);
            }

            bool found = userAccountData.GetAccountByIdAccount(request.AccountId, out AccountDto accountDto);

            if (!found)
            {
                Logger.WarnFormat("ConfirmEmailAddressWithVerificationCode failed: account not found for accountId '{0}'.", 
                    request.AccountId);
                throw Faults.Create(
                    FAULT_CODE_ACCOUNT_NOT_FOUND,
                    FAULT_MESSAGE_ACCOUNT_NOT_FOUND);
            }

            if (accountDto.IsEmailVerified)
            {
                Logger.InfoFormat("ConfirmEmailAddressWithVerificationCode skipped: email already verified for accountId '{0}'.", 
                    request.AccountId);
                return new VerifyEmailResponse { Success = true };
            }

            var token = emailVerificationData.GetLatestTokenByAccountId(request.AccountId, currentUtcTimestamp)
                ?? throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED);

            byte[] codeHash = CodeGenerator.ComputeSha256Hash(code);

            if (!AreByteSequencesEqualInConstantTime(codeHash, token.CODEHASH))
            {
                Logger.WarnFormat("ConfirmEmailAddressWithVerificationCode failed: invalid code for accountId '{0}'.", 
                    request.AccountId);

                emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(token.TOKENID, currentUtcTimestamp);

                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED);
            }

            int consumedRows = emailVerificationData.ConsumeToken(token.TOKENID);

            if (consumedRows == 0)
            {
                Logger.WarnFormat("ConfirmEmailAddressWithVerificationCode failed: token already consumed or not found for tokenId '{0}'.", 
                    token.TOKENID);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED);
            }

            userAccountData.MarkEmailVerified(request.AccountId, currentUtcTimestamp);

            Logger.InfoFormat("ConfirmEmailAddressWithVerificationCode succeeded for accountId '{0}'.", 
                request.AccountId);

            return new VerifyEmailResponse { Success = true };
        }

        public void ResendEmailVerificationCode(ResendVerificationRequest request)
        {
            DateTime currentUtcTimestamp = DateTime.UtcNow;

            Logger.InfoFormat("ResendEmailVerificationCode attempt for accountId '{0}'.", request.AccountId);

            bool found = userAccountData.GetAccountByIdAccount(request.AccountId, out AccountDto accountDto);

            if (!found)
            {
                Logger.WarnFormat("ResendEmailVerificationCode failed: account not found for accountId '{0}'.", 
                    request.AccountId);
                throw Faults.Create(
                    FAULT_CODE_ACCOUNT_NOT_FOUND,
                    FAULT_MESSAGE_ACCOUNT_NOT_FOUND);
            }

            if (accountDto.IsEmailVerified)
            {
                Logger.InfoFormat("ResendEmailVerificationCode skipped: email already verified for accountId '{0}'.", 
                    request.AccountId);
                return;
            }

            var (perMinute, withinHourCap, _, _) = emailVerificationData.GetEmailVerificationResendLimits(
                request.AccountId,
                currentUtcTimestamp);

            if (perMinute)
            {
                Logger.WarnFormat("ResendEmailVerificationCode blocked by per-minute limit for accountId '{0}'.", 
                    request.AccountId);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT);
            }

            if (!withinHourCap)
            {
                Logger.WarnFormat("ResendEmailVerificationCode blocked by hourly limit for accountId '{0}'.", 
                    request.AccountId);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED);
            }

            var verificationCodeResult = CreateVerificationCodeOrFault();

            var createTokenArgs = new CreateEmailTokenArgs
            {
                AccountId = request.AccountId,
                CodeHash = verificationCodeResult.HashCode,
                NowUtc = currentUtcTimestamp,
                LifeSpan = VerificationCodeLifeTime
            };

            emailVerificationData.AddVerificationToken(createTokenArgs);

            TrySendVerificationEmailOrThrow(accountDto.Email, verificationCodeResult.PlainCode);

            Logger.InfoFormat("ResendEmailVerificationCode succeeded for accountId '{0}'.", request.AccountId);
        }

        private static void TrySendVerificationEmailOrThrow(string email, string code)
        {
            try
            {
                Logger.InfoFormat("Sending verification email to '{0}'.", email);
                new VerificationEmailSender().SendVerificationCode(email, code);
                Logger.InfoFormat("Verification email sent successfully to '{0}'.", email);
            }
            catch (EmailSendException ex)
            {
                Logger.Error("EmailSendException while sending verification email.", ex);
                throw Faults.Create(ex.Code, ex.Message, ex);
            }
            catch (ArgumentException ex) when (ex.ParamName == "recipientEmailAddress")
            {
                Logger.Warn("Invalid recipient email address while sending verification email.", ex);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_RECIPIENT_INVALID,
                    FAULT_MESSAGE_EMAIL_RECIPIENT_INVALID,
                    ex);
            }
            catch (ArgumentException ex) when (ex.ParamName == "verificationCode")
            {
                Logger.Warn("Invalid verification code format while sending verification email.", ex);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT,
                    ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Fatal("SMTP configuration missing or invalid while sending verification email.", ex);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_SMTP_CONFIGURATION_MISSING,
                    FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_MISSING,
                    ex);
            }
            catch (AuthenticationException ex)
            {
                Logger.Fatal("SMTP authentication failed while sending verification email.", ex);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_SMTP_AUTHENTICATION_FAILED,
                    FAULT_MESSAGE_EMAIL_SMTP_AUTHENTICATION_FAILED,
                    ex);
            }
            catch (SmtpException ex) when (
                ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst ||
                ex.StatusCode == SmtpStatusCode.ClientNotPermitted ||
                ex.StatusCode == SmtpStatusCode.CommandNotImplemented)
            {
                Logger.Fatal("SMTP configuration error while sending verification email.", ex);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_SMTP_CONFIGURATION_ERROR,
                    FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_ERROR,
                    ex);
            }
            catch (SmtpException ex) when (
                ex.StatusCode == SmtpStatusCode.GeneralFailure ||
                ex.StatusCode == SmtpStatusCode.TransactionFailed ||
                ex.StatusCode == SmtpStatusCode.MailboxBusy ||
                ex.StatusCode == SmtpStatusCode.InsufficientStorage)
            {
                Logger.Error("SMTP unavailable while sending verification email.", ex);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_SMTP_UNAVAILABLE,
                    FAULT_MESSAGE_EMAIL_SMTP_UNAVAILABLE,
                    ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while sending verification email.", ex);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_SEND_FAILED,
                    FAULT_MESSAGE_EMAIL_SEND_FAILED,
                    ex);
            }
        }

        private static VerificationCodeResult CreateVerificationCodeOrFault()
        {
            try
            {
                string code = CodeGenerator.GenerateNumericCode();
                byte[] hashCode = CodeGenerator.ComputeSha256Hash(code);

                return new VerificationCodeResult(code, hashCode);
            }
            catch (ArgumentNullException ex)
            {
                Logger.Error("Crypto random generator unavailable (ArgumentNullException) while generating verification code.", ex);
                throw Faults.Create(
                    FAULT_CODE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    FAULT_MESSAGE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Error("Verification code generation failed (ArgumentOutOfRangeException).", ex);
                throw Faults.Create(
                    FAULT_CODE_VERIFICATION_CODE_GENERATION_FAILED,
                    FAULT_MESSAGE_VERIFICATION_CODE_GENERATION_FAILED,
                    ex);
            }
            catch (CryptographicException ex)
            {
                Logger.Error("Crypto random generator unavailable (CryptographicException) while generating verification code.", ex);
                throw Faults.Create(
                    FAULT_CODE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    FAULT_MESSAGE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    ex);
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

        public PasswordRecoveryResponse SendPasswordRecoveryCode(PasswordRecoveryRequest request)
        {
            if (request == null)
            {
                Logger.Warn("SendPasswordRecoveryCode request is null.");
                throw Faults.Create(FAULT_CODE_REQUEST_NULL, FAULT_MESSAGE_REGISTER_REQUEST_NULL);
            }

            string email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            DateTime currentUtcTimestamp = DateTime.UtcNow;

            Logger.InfoFormat("Password recovery requested for email '{0}'.", email);

            long accountId = userAccountData.GetAccountIdByEmail(email);

            if (accountId <= 0)
            {
                Logger.WarnFormat("Password recovery: Account not found for email '{0}'. Returning ambiguous success.", email);
                return new PasswordRecoveryResponse
                {
                    Success = true,
                    Message = "If the email is registered, a recovery code has been sent."
                };
            }

            var (perMinute, withinHourCap, _, _) = emailVerificationData.GetEmailVerificationResendLimits(
                accountId,
                currentUtcTimestamp);

            if (perMinute)
            {
                Logger.WarnFormat("Password recovery blocked by per-minute limit for email '{0}'.", email);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT);
            }

            if (!withinHourCap)
            {
                Logger.WarnFormat("Password recovery blocked by hourly limit for email '{0}'.", email);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED,
                    FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED);
            }

            var verificationCodeResult = CreateVerificationCodeOrFault();

            var createTokenArgs = new CreateEmailTokenArgs
            {
                AccountId = accountId,
                CodeHash = verificationCodeResult.HashCode,
                NowUtc = currentUtcTimestamp,
                LifeSpan = VerificationCodeLifeTime
            };

            emailVerificationData.AddVerificationToken(createTokenArgs);

            TrySendVerificationEmailOrThrow(email, verificationCodeResult.PlainCode);

            Logger.InfoFormat("Password recovery code sent successfully to '{0}'.", email);

            return new PasswordRecoveryResponse
            {
                Success = true,
                Message = "Verification code sent to your email."
            };
        }

        public bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request)
        {
            if (request == null) throw Faults.Create(FAULT_CODE_REQUEST_NULL, "Request cannot be null.");

            string email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            string code = request.VerificationCode;
            string newPassword = request.NewPassword;
            DateTime nowUtc = DateTime.UtcNow;

            Logger.InfoFormat("Password reset attempt for email '{0}'.", email);

            long accountId = userAccountData.GetAccountIdByEmail(email);
            if (accountId <= 0)
            {
                throw Faults.Create(FAULT_CODE_ACCOUNT_NOT_FOUND, FAULT_MESSAGE_ACCOUNT_NOT_FOUND);
            }

            var token = emailVerificationData.GetLatestTokenByAccountId(accountId, nowUtc);

            if (token == null)
            {
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    "The verification code has expired or does not exist.");
            }

            byte[] inputHash = CodeGenerator.ComputeSha256Hash(code);

            if (!AreByteSequencesEqualInConstantTime(inputHash, token.CODEHASH))
            {
                emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(token.TOKENID, nowUtc);
                throw Faults.Create(
                    FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    "Invalid verification code.");
            }

            emailVerificationData.ConsumeToken(token.TOKENID);

            byte[] newPasswordHash = PasswordHasher.HashPassword(newPassword);

            bool success = userAccountData.UpdatePasswordOnly(accountId, newPasswordHash);

            if (success)
            {
                Logger.InfoFormat("Password updated successfully for accountId '{0}'.", accountId);
                return true;
            }
            else
            {
                throw Faults.Create(FAULT_CODE_UNEXPECTED_ERROR, "Could not update password in database.");
            }
        }
    }
}
