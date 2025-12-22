using System;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using GuessWhoServices.Repositories.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Parameters;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class LoginCoordinator : ILoginCoordinator
    {
        private readonly ILoginManager _loginManager;
        private readonly IGameSessionManager _sessionManager;
        private readonly IUserAccountRepository _accountRepository;

        public LoginCoordinator(
            ILoginManager loginManager,
            IGameSessionManager sessionManager,
            IUserAccountRepository accountRepository)
        {
            _loginManager = loginManager ?? throw new ArgumentNullException(nameof(loginManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        }

        public UserSessionLoginResult LoginAndInitializeSession(LoginArgs args)
        {
            var loginResult = _loginManager.Login(args);

            if (!loginResult.IsSuccess)
            {
                return loginResult;
            }

            _sessionManager.TerminateActiveSessions(loginResult.Profile.UserId);

            _accountRepository.MarkUserProfileActive(loginResult.Profile.UserId);

            return loginResult;
        }

        public bool Logout(long userProfileId)
        {
            if (userProfileId <= 0) return false;

            _sessionManager.TerminateActiveSessions(userProfileId);

            return _accountRepository.MarkUserProfileInactive(userProfileId);
        }
    }
}