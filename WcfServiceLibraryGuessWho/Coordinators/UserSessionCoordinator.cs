using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.DataAccess.Match;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Enums;
using log4net;
using System;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public class UserSessionCoordinator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserSessionCoordinator));

        private const string LOG_CONTEXT_FORCE_LEAVE_EXECUTED =
            "LoginUser: ForceLeaveAllMatchesForUser executed. UserId={0}, cleaned={1}";

        private readonly UserAccountData accountData;
        private readonly MatchData matchData;

        public UserSessionCoordinator(UserAccountData accountData, MatchData matchData)
        {
            this.accountData = accountData ?? 
                throw new ArgumentNullException(nameof(accountData));
            this.matchData = matchData ?? 
                throw new ArgumentNullException(nameof(matchData));
        }

        public UserSessionLoginResult LoginUser(LoginRequest request)
        {
            UserSessionLoginResult result;

            AccountSearchParameters accountSearchParameters = new AccountSearchParameters
            {
                Email = request.Email
            };

            AccountProfileResult accountProfileResult =
                accountData.GetAccountWithProfileForLogin(accountSearchParameters);

            if (accountProfileResult.Status != AccountProfileStatus.Success)
            {
                result = MapAccountProfileStatusToLoginResult(accountProfileResult.Status);
            }
            else
            {
                AccountDto account = accountProfileResult.Account;
                UserProfileDto profile = accountProfileResult.Profile;

                bool isPasswordValid = PasswordHasher.Verify(request.Password, account.PasswordHash);

                if (!isPasswordValid)
                {
                    result = UserSessionLoginResult.Fail(UserSessionLoginStatus.InvalidCredentials);
                }
                else
                {
                    bool isLastLoginUpdated = accountData.UpdateLastLoginUtc(accountSearchParameters);

                    if (!isLastLoginUpdated)
                    {
                        result = UserSessionLoginResult.Fail(UserSessionLoginStatus.LastLoginUpdateFailed);
                    }
                    else
                    {
                        CleanUserMatches(profile.UserId);

                        result = UserSessionLoginResult.Ok(account, profile);
                    }
                }
            }

            return result;
        }

        private static UserSessionLoginResult MapAccountProfileStatusToLoginResult(AccountProfileStatus status)
        {
            UserSessionLoginResult result;

            switch (status)
            {
                case AccountProfileStatus.Locked:
                    result = UserSessionLoginResult.Fail(UserSessionLoginStatus.AccountLocked);
                    break;

                case AccountProfileStatus.ProfileAlreadyActive:
                    result = UserSessionLoginResult.Fail(UserSessionLoginStatus.ProfileAlreadyActive);
                    break;

                case AccountProfileStatus.NotFoundOrDeleted:
                    result = UserSessionLoginResult.Fail(UserSessionLoginStatus.AccountNotFoundOrDeleted);
                    break;

                case AccountProfileStatus.ProfileNotFound:
                    result = UserSessionLoginResult.Fail(UserSessionLoginStatus.ProfileNotFound);
                    break;

                default:
                    result = UserSessionLoginResult.Fail(UserSessionLoginStatus.AccountNotFoundOrDeleted);
                    break;
            }

            return result;
        }

        private void CleanUserMatches(long userId)
        {
            bool cleaned = matchData.ForceLeaveAllMatchesForUser(userId);

            Logger.InfoFormat(LOG_CONTEXT_FORCE_LEAVE_EXECUTED, userId, cleaned);
        }
    }
}
