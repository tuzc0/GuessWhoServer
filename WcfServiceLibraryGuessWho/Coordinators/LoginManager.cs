using GuessWhoCore.Contracts.Faults;
using GuessWhoServerDomain.Domain.Enums;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.Sessions;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServerDomain.Domain.Settings;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Parameters.InternalDtos;
using WcfServiceLibraryGuessWho.Errors;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class LoginManager : ManagerBase, ILoginManager
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(LoginManager));

        private const string EMPTY = "";

        private const string LOG_CTX_LOGIN = "LoginManager.Login";
        private const string LOG_CTX_UPDATE_LAST_LOGIN = "LoginManager.UpdateLastLoginUtc";
        private const string LOG_CTX_DUMMY_HASH = "LoginManager.DummyVerify";

        private const string DEFAULT_DUMMY_PASSWORD = "DUMMY_PASSWORD";

        private readonly IUserAccountRepository accountRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly byte[] dummyPasswordHash;

        public LoginManager(
            IUserAccountRepository accountRepository,
            IPasswordHasher passwordHasher,
            UserSecuritySettings securitySettings)
        {
            this.accountRepository = accountRepository ??
                throw new ArgumentNullException(nameof(accountRepository));
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

                    AccountProfileRecordResult result =
                        accountRepository.GetAccountWithProfileForLogin(searchParams, nowUtc);

                    if (result == null || result.Status == AccountProfileStatus.NotFoundOrDeleted)
                    {
                        BurnTimeForTimingResistance(password);

                        Logger.WarnFormat(
                            "{0}: invalid credentials for email '{1}'.",
                            LOG_CTX_LOGIN,
                            normalizedEmail);

                        throw CreateInvalidCredentialsFault();
                    }

                    if (result.Status == AccountProfileStatus.Locked)
                    {
                        Logger.WarnFormat(
                            "{0}: account locked for email '{1}'.",
                            LOG_CTX_LOGIN,
                            normalizedEmail);

                        throw FaultsFactory.Create(
                            LoginFaultKeys.CODE_ACCOUNT_LOCKED,
                            LoginFaultKeys.MSG_ACCOUNT_LOCKED,
                            LoginFaultKeys.FALLBACK_ACCOUNT_LOCKED);
                    }

                    bool isPasswordValid = passwordHasher.VerifyPassword(password, result.Account.PasswordHash);

                    if (!isPasswordValid)
                    {
                        Logger.WarnFormat(
                            "{0}: invalid credentials for email '{1}'.",
                            LOG_CTX_LOGIN,
                            normalizedEmail);

                        throw CreateInvalidCredentialsFault();
                    }

                    bool lastLoginUpdated = accountRepository.UpdateLastLoginUtc(searchParams, nowUtc);

                    if (!lastLoginUpdated)
                    {
                        Logger.WarnFormat(
                            "{0}: could not update last login utc for email '{1}'.",
                            LOG_CTX_UPDATE_LAST_LOGIN,
                            normalizedEmail);
                    }

                    return SessionLoginResult.CreateSuccessful(result.Account, result.Profile);
                });
        }

        private void BurnTimeForTimingResistance(string password)
        {
            try
            {
                _ = passwordHasher.VerifyPassword(password ?? EMPTY, dummyPasswordHash);
            }
            catch (Exception ex)
            {
                Logger.Warn(LOG_CTX_DUMMY_HASH, ex);
            }
        }

        private byte[] BuildDummyHashOrFallback(string dummyHashBase64)
        {
            byte[] parsed = ParseDummyHashOrEmpty(dummyHashBase64);

            if (parsed.Length > 0)
            {
                return parsed;
            }

            try
            {
                return passwordHasher.HashPassword(DEFAULT_DUMMY_PASSWORD);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static byte[] ParseDummyHashOrEmpty(string base64)
        {
            string safe = (base64 ?? EMPTY).Trim();

            if (string.IsNullOrWhiteSpace(safe))
            {
                return Array.Empty<byte>();
            }

            try
            {
                return Convert.FromBase64String(safe);
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
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
