using GuessWhoCore.Contracts.Faults;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServerDomain.Domain.Settings;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServerDomain.Domain.Models.Session;
using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoDataAccess.Data.Factories;

namespace GuessWhoServices.Coordinators
{
    public sealed class LoginManager : ManagerBase, ILoginManager
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(LoginManager));

        private const string EMPTY = "";

        private const string LOG_CTX_LOGIN = "LoginManager.Login";
        private const string LOG_CTX_UPDATE_LAST_LOGIN = "LoginManager.UpdateLastLoginUtc";
        private const string LOG_CTX_DUMMY_HASH = "LoginManager.DummyVerify";

        private const string DEFAULT_DUMMY_PASSWORD = "DUMMY_PASSWORD";

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IPasswordHasher passwordHasher;
        private readonly byte[] dummyPasswordHash;

        public LoginManager(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IPasswordHasher passwordHasher,
            UserSecuritySettings securitySettings)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.passwordHasher = passwordHasher ??
                throw new ArgumentNullException(nameof(passwordHasher));

            if (securitySettings == null)
            {
                throw new ArgumentNullException(nameof(securitySettings));
            }

            dummyPasswordHash = BuildDummyHashOrFallback(securitySettings.DummyPasswordHashBase64);
        }

        public SessionLoginResult Login(LoginArgs loginArgs)
        {
            return ExecuteService(
                LOG_CTX_LOGIN,
                () =>
                {
                    ValidateLoginArgsOrThrow(loginArgs);

                    string normalizedEmail = NormalizeEmail(loginArgs.Email);
                    string password = loginArgs.Password ?? EMPTY;

                    var searchParams = new AccountSearchParameters
                    {
                        Email = normalizedEmail
                    };

                    DateTime nowUtc = DateTime.UtcNow;

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

                    AccountProfileRecordResult result =
                        unitOfWork.UserAccounts.GetAccountWithProfileForLogin(searchParams, nowUtc);

                    if (result == null || result.Status == AccountProfileStatus.NotFoundOrDeleted)
                    {
                        BurnTimeForTimingResistance(password);

                        Logger.WarnFormat("{0}: invalid credentials for email '{1}'.", LOG_CTX_LOGIN, normalizedEmail);

                        throw CreateInvalidCredentialsFault();
                    }

                    if (result.Status == AccountProfileStatus.Locked)
                    {
                        Logger.WarnFormat("{0}: account locked for email '{1}'.", LOG_CTX_LOGIN, normalizedEmail);

                        throw FaultsFactory.Create(
                            LoginFaultKeys.CODE_ACCOUNT_LOCKED,
                            LoginFaultKeys.MSG_ACCOUNT_LOCKED,
                            LoginFaultKeys.FALLBACK_ACCOUNT_LOCKED);
                    }

                    bool isPasswordValid = passwordHasher.VerifyPassword(password, result.Account.PasswordHash);

                    if (!isPasswordValid)
                    {
                        BurnTimeForTimingResistance(password);

                        Logger.WarnFormat("{0}: invalid credentials for email '{1}'.", LOG_CTX_LOGIN, normalizedEmail);

                        throw CreateInvalidCredentialsFault();
                    }

                    bool lastLoginUpdated = unitOfWork.UserAccounts.UpdateLastLoginUtc(searchParams, nowUtc);

                    if (lastLoginUpdated)
                    {
                        unitOfWork.Flush();
                    }
                    else
                    {
                        Logger.WarnFormat("{0}: could not update last login utc for email '{1}'.", LOG_CTX_UPDATE_LAST_LOGIN,
                            normalizedEmail);
                    }

                    return SessionLoginResult.CreateSuccessful(result.Account, result.Profile);
                });
        }

        private void BurnTimeForTimingResistance(string password)
        {
            try
            {
                bool dummyResult = passwordHasher.VerifyPassword(password ?? EMPTY, dummyPasswordHash);

                if (dummyResult)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                // Security note:
                // This catch is intentionally broad to reduce timing side-channel leakage.
                // If dummy verification throws (e.g., malformed stored hash), propagating the exception would
                // fail fast and could reveal information about the authentication path through response time.
                // We log the exception for diagnostics and swallow it to keep behavior/time consistent.
                Logger.Warn(LOG_CTX_DUMMY_HASH, ex);
            }
        }

        private byte[] BuildDummyHashOrFallback(string dummyHashBase64)
        {
            HashAttemptResult parsed = ParseConfiguredDummyHash(dummyHashBase64);

            if (parsed.IsSuccess)
            {
                return parsed.Hash;
            }

            HashAttemptResult computed = ComputeDummyHash();

            if (computed.IsSuccess)
            {
                return computed.Hash;
            }

            return Array.Empty<byte>();
        }

        private HashAttemptResult ParseConfiguredDummyHash(string base64)
        {
            string trimmedBase64 = (base64 ?? EMPTY).Trim();

            if (string.IsNullOrWhiteSpace(trimmedBase64))
            {
                return HashAttemptResult.Fail();
            }

            try
            {
                byte[] candidate = Convert.FromBase64String(trimmedBase64);

                if (candidate != null && candidate.Length > 0)
                {
                    return HashAttemptResult.Ok(candidate);
                }

                return HashAttemptResult.Fail();
            }
            catch (FormatException ex)
            {
                Logger.Warn(LOG_CTX_DUMMY_HASH, ex);
                return HashAttemptResult.Fail();
            }
        }

        private HashAttemptResult ComputeDummyHash()
        {
            try
            {
                byte[] candidate = passwordHasher.HashPassword(DEFAULT_DUMMY_PASSWORD);

                if (candidate != null && candidate.Length > 0)
                {
                    return HashAttemptResult.Ok(candidate);
                }

                return HashAttemptResult.Fail();
            }
            catch (ArgumentException ex)
            {
                Logger.Warn(LOG_CTX_DUMMY_HASH, ex);
                return HashAttemptResult.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn(LOG_CTX_DUMMY_HASH, ex);
                return HashAttemptResult.Fail();
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                Logger.Warn(LOG_CTX_DUMMY_HASH, ex);
                return HashAttemptResult.Fail();
            }
        }

        private readonly record struct HashAttemptResult(bool IsSuccess, byte[] Hash)
        {
            public static HashAttemptResult Ok(byte[] hash)
            {
                byte[] safeHash = hash ?? Array.Empty<byte>();
                return new HashAttemptResult(safeHash.Length > 0, safeHash);
            }

            public static HashAttemptResult Fail()
            {
                return new HashAttemptResult(false, Array.Empty<byte>());
            }
        }

        private static void ValidateLoginArgsOrThrow(LoginArgs args)
        {
            if (args == null)
            {
                throw FaultsFactory.Create(
                    LoginFaultKeys.CODE_REQUEST_NULL,
                    LoginFaultKeys.MSG_REQUEST_NULL,
                    LoginFaultKeys.FALLBACK_REQUEST_NULL);
            }

            string normalizedEmail = NormalizeEmail(args.Email);
            string password = args.Password ?? EMPTY;

            if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
            {
                throw CreateInvalidCredentialsFault();
            }
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? EMPTY).Trim().ToLowerInvariant();
        }

        private static FaultException<ServiceFault> CreateInvalidCredentialsFault()
        {
            return FaultsFactory.Create(
                LoginFaultKeys.CODE_INVALID_CREDENTIALS,
                LoginFaultKeys.MSG_INVALID_CREDENTIALS,
                LoginFaultKeys.FALLBACK_INVALID_CREDENTIALS);
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
