using ConsoleGuessWho.Infraestructure.Settings;
using ConsoleGuessWho.Infraestructure.Wcf;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Settings;
using GuessWhoServices.Communication.Email;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Coordinators;
using GuessWhoServices.Coordinators.EmailVerification;
using GuessWhoServices.Coordinators.Match;
using GuessWhoServices.Coordinators.Tournament;
using GuessWhoServices.Infrastructure;
using GuessWhoServices.Security;
using GuessWhoServices.Services;
using GuessWhoServices.Services.Configuration;
using GuessWhoServices.Services.MatchApplication;
using log4net;
using log4net.Config;
using System;
using System.ServiceModel;
using static GuessWhoServices.Infrastructure.LobbySubscriptionStore;

[assembly: XmlConfigurator(Watch = true)]
namespace ConsoleGuessWho
{
    internal static class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        private const string SERVICE_HOST_STARTING_MESSAGE = "Starting Console host.";
        private const string SERVICE_HOST_STARTED_MESSAGE = "Console host started";
        private const string SERVICE_HOST_STOPPING_MESSAGE = "Stopping Console host";
        private const string SERVICE_HOST_STOPPED_MESSAGE = "Console host stopped";
        private const string SERVICE_HOST_FATAL_ERROR_MESSAGE = "Fatal error in host";
        private const string SERVER_ONLINE_MESSAGE = "Servidor en línea. Presiona ENTER para cerrar.";
        private const string FATAL_CONSOLE_PREFIX = "ERROR FATAL: ";

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            Logger.Info(SERVICE_HOST_STARTING_MESSAGE);

            try
            {
                HostComposition composition = BuildComposition();

                ServiceHost hostUser = CreateHost<UserService>(() => CreateUserService(composition));
                ServiceHost hostLogin = CreateHost<LoginService>(() => CreateLoginService(composition));
                ServiceHost hostMatch = CreateHost<MatchService>(() => CreateMatchService(composition));
                ServiceHost hostUpdateProfile = CreateHost<UpdateProfileService>(() => CreateUpdateProfileService(composition));
                ServiceHost hostFriend = CreateHost<FriendService>(() => CreateFriendService(composition));
                ServiceHost hostLeaderboard = CreateHost<LeaderboardService>(() => CreateLeaderboardService(composition));
                ServiceHost hostTournament = CreateHost<TournamentService>(() => CreateTournamentService(composition));

                using (hostUser)
                using (hostLogin)
                using (hostMatch)
                using (hostUpdateProfile)
                using (hostFriend)
                using (hostLeaderboard)
                using (hostTournament)
                {
                    hostUser.Open();
                    hostLogin.Open();
                    hostMatch.Open();
                    hostUpdateProfile.Open();
                    hostFriend.Open();
                    hostLeaderboard.Open();
                    hostTournament.Open();

                    Logger.Info(SERVICE_HOST_STARTED_MESSAGE);
                    Console.WriteLine(SERVER_ONLINE_MESSAGE);
                    Console.ReadLine();

                    Logger.Info(SERVICE_HOST_STOPPING_MESSAGE);
                }

                Logger.Info(SERVICE_HOST_STOPPED_MESSAGE);
            }
            catch (Exception ex)
            {
                Logger.Fatal(SERVICE_HOST_FATAL_ERROR_MESSAGE, ex);
                Console.WriteLine(FATAL_CONSOLE_PREFIX + ex.Message);
                Console.ReadLine();
            }
        }

        private static HostComposition BuildComposition()
        {
            GuessWhoDbContextFactory contextFactory = new GuessWhoDbContextFactory();
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory = new GuessWhoUnitOfWorkFactory(contextFactory);

            UserSecuritySettings securitySettings = UserSecuritySettingsLoader.Load();
            IPasswordHasher passwordHasher = new PasswordHasher();
            IVerificationCodeService verificationCodeService = new VerificationCodeService();

            SmtpSettings smtpSettings = SmtpSettingsLoader.Load();
            smtpSettings.Validate();
            IEmailSender emailSender = new SmtpEmailSender(smtpSettings);

            VerificationCodeEmailBuilder verificationCodeEmailBuilder = new VerificationCodeEmailBuilder();
            MatchInvitationEmailBuilder matchInvitationEmailBuilder = new MatchInvitationEmailBuilder();

            IMatchDisconnectHandler disconnectHandler = new MatchDisconnectHandler(unitOfWorkFactory, LogManager.GetLogger(typeof(MatchDisconnectHandler)));
            ILobbySubscriptionStore lobbySubscriptionStore = new LobbySubscriptionStore(disconnectHandler);
            IMatchCallbackDispatcher matchCallbackDispatcher = new MatchCallbackDispatcher(lobbySubscriptionStore);
            ILobbySubscriptionOperations lobbySubscriptionOperations = new LobbySubscriptionOperations(lobbySubscriptionStore);

            ITournamentDisconnectHandler tournamentDisconnectHandler = new TournamentSubscriptionStore.TournamentDisconnectHandler(unitOfWorkFactory, LogManager.GetLogger(typeof(TournamentSubscriptionStore.TournamentDisconnectHandler)));
            ITournamentSubscriptionStore tournamentSubscriptionStore = new TournamentSubscriptionStore(tournamentDisconnectHandler);
            ITournamentCallbackDispatcher tournamentCallbackDispatcher = new TournamentCallbackDispatcher(tournamentSubscriptionStore);
            ITournamentSubscriptionOperations tournamentSubscriptionOperations = new TournamentSubscriptionOperations(tournamentSubscriptionStore, tournamentCallbackDispatcher);

            TournamentLobbyLogic sharedTournamentLogic = new TournamentLobbyLogic(unitOfWorkFactory, tournamentSubscriptionOperations);

            var draft = new HostCompositionDraft
            {
                UnitOfWorkFactory = unitOfWorkFactory,
                SecuritySettings = securitySettings,
                PasswordHasher = passwordHasher,
                VerificationCodeService = verificationCodeService,
                EmailSender = emailSender,
                VerificationCodeEmailBuilder = verificationCodeEmailBuilder,
                MatchInvitationEmailBuilder = matchInvitationEmailBuilder,
                LobbySubscriptionStore = lobbySubscriptionStore,
                MatchCallbackDispatcher = matchCallbackDispatcher,
                LobbySubscriptionOperations = lobbySubscriptionOperations,
                TournamentSubscriptionStore = tournamentSubscriptionStore,
                TournamentCallbackDispatcher = tournamentCallbackDispatcher,
                TournamentSubscriptionOperations = tournamentSubscriptionOperations,
                TournamentLobbyLogic = sharedTournamentLogic
            };

            return new HostComposition(draft);
        }

        private static ServiceHost CreateHost<TService>(Func<TService> serviceFactory)
        {
            if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));
            ServiceHost host = new ServiceHost(typeof(TService));
            host.Description.Behaviors.Add(new DelegateServiceBehavior(() => serviceFactory()));
            return host;
        }

        private static UserService CreateUserService(HostComposition composition)
        {
            var domainServiceArgs = new EmailVerificationDomainServiceArgs(composition.UnitOfWorkFactory, composition.VerificationCodeService, composition.SecuritySettings, LogManager.GetLogger(typeof(EmailVerificationDomainService)));
            var emailVerificationDomainService = new EmailVerificationDomainService(domainServiceArgs);
            var registrationManager = new UserRegistrationManager(composition.UnitOfWorkFactory, composition.EmailSender, composition.VerificationCodeEmailBuilder, composition.PasswordHasher, composition.VerificationCodeService, composition.SecuritySettings.VerificationCodeLifetime);
            var emailVerificationManager = new EmailVerificationManager(composition.UnitOfWorkFactory, composition.VerificationCodeService, composition.EmailSender, composition.VerificationCodeEmailBuilder, emailVerificationDomainService);
            var passwordRecoveryManager = new PasswordRecoveryManager(composition.UnitOfWorkFactory, composition.EmailSender, composition.VerificationCodeEmailBuilder, composition.VerificationCodeService, emailVerificationDomainService, composition.PasswordHasher);
            return new UserService(registrationManager, emailVerificationManager, passwordRecoveryManager);
        }

        private static LoginService CreateLoginService(HostComposition composition)
        {
            var loginManager = new LoginManager(composition.UnitOfWorkFactory, composition.PasswordHasher, composition.SecuritySettings);
            var loginCoordinator = new LoginCoordinator(loginManager, composition.UnitOfWorkFactory);
            return new LoginService(loginCoordinator);
        }

        private static UpdateProfileService CreateUpdateProfileService(HostComposition composition)
        {
            var manager = new UpdateProfileManager(composition.UnitOfWorkFactory, composition.PasswordHasher);
            return new UpdateProfileService(manager);
        }

        private static FriendService CreateFriendService(HostComposition composition)
        {
            var manager = new FriendshipManager(composition.UnitOfWorkFactory);
            return new FriendService(manager);
        }

        private static LeaderboardService CreateLeaderboardService(HostComposition composition)
        {
            var manager = new LeaderboardManager(composition.UnitOfWorkFactory);
            return new LeaderboardService(manager);
        }

        private static MatchService CreateMatchService(HostComposition composition)
        {
            var tournamentLogic = composition.TournamentLobbyLogic;

            MatchLobbyLogic lobbyLogic = new MatchLobbyLogic(composition.UnitOfWorkFactory, composition.LobbySubscriptionOperations, composition.EmailSender, composition.MatchInvitationEmailBuilder);
            MatchLifecycleLogic lifecycleLogic = new MatchLifecycleLogic(composition.UnitOfWorkFactory, composition.MatchCallbackDispatcher, tournamentLogic);
            MatchDeckLogic deckLogic = new MatchDeckLogic(composition.UnitOfWorkFactory);
            MatchSecretCharacterLogic secretLogic = new MatchSecretCharacterLogic(composition.UnitOfWorkFactory, composition.MatchCallbackDispatcher);
            MatchQuestionLogic questionLogic = new MatchQuestionLogic(composition.UnitOfWorkFactory, composition.MatchCallbackDispatcher);
            MatchPassTurnLogic passTurnLogic = new MatchPassTurnLogic(composition.UnitOfWorkFactory);
            MatchGuessingLogic guessingLogic = new MatchGuessingLogic(composition.UnitOfWorkFactory, tournamentLogic);

            var deps = new MatchService.MatchServiceDependencies
            {
                LobbyLogic = lobbyLogic,
                LifecycleLogic = lifecycleLogic,
                DeckLogic = deckLogic,
                SecretCharacterLogic = secretLogic,
                QuestionLogic = questionLogic,
                PassTurnLogic = passTurnLogic,
                GuessingLogic = guessingLogic,
                CallbackDispatcher = composition.MatchCallbackDispatcher,
                TournamentLobbyLogic = tournamentLogic
            };
            return new MatchService(deps);
        }

        private static TournamentService CreateTournamentService(HostComposition composition)
        {
            var deps = new TournamentService.TournamentServiceDependencies
            {
                LobbyLogic = composition.TournamentLobbyLogic,
                SubscriptionOperations = composition.TournamentSubscriptionOperations
            };
            return new TournamentService(deps);
        }

        private sealed class HostCompositionDraft
        {
            public IGuessWhoUnitOfWorkFactory UnitOfWorkFactory { get; set; }
            public UserSecuritySettings SecuritySettings { get; set; }
            public IPasswordHasher PasswordHasher { get; set; }
            public IVerificationCodeService VerificationCodeService { get; set; }
            public IEmailSender EmailSender { get; set; }
            public VerificationCodeEmailBuilder VerificationCodeEmailBuilder { get; set; }
            public MatchInvitationEmailBuilder MatchInvitationEmailBuilder { get; set; }
            public ILobbySubscriptionStore LobbySubscriptionStore { get; set; }
            public IMatchCallbackDispatcher MatchCallbackDispatcher { get; set; }
            public ILobbySubscriptionOperations LobbySubscriptionOperations { get; set; }
            public ITournamentSubscriptionStore TournamentSubscriptionStore { get; set; }
            public ITournamentCallbackDispatcher TournamentCallbackDispatcher { get; set; }
            public ITournamentSubscriptionOperations TournamentSubscriptionOperations { get; set; }
            public TournamentLobbyLogic TournamentLobbyLogic { get; set; }
        }

        private sealed class HostComposition
        {
            private const string ERROR_MISSING_DEPENDENCY_FORMAT = "Missing required dependency in HostCompositionDraft: {0}.";

            public HostComposition(HostCompositionDraft draft)
            {
                if (draft == null) throw new ArgumentNullException(nameof(draft));
                UnitOfWorkFactory = draft.UnitOfWorkFactory ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.UnitOfWorkFactory)), nameof(draft));
                SecuritySettings = draft.SecuritySettings ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.SecuritySettings)), nameof(draft));
                PasswordHasher = draft.PasswordHasher ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.PasswordHasher)), nameof(draft));
                VerificationCodeService = draft.VerificationCodeService ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.VerificationCodeService)), nameof(draft));
                EmailSender = draft.EmailSender ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.EmailSender)), nameof(draft));
                VerificationCodeEmailBuilder = draft.VerificationCodeEmailBuilder ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.VerificationCodeEmailBuilder)), nameof(draft));
                MatchInvitationEmailBuilder = draft.MatchInvitationEmailBuilder ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.MatchInvitationEmailBuilder)), nameof(draft));
                LobbySubscriptionStore = draft.LobbySubscriptionStore ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.LobbySubscriptionStore)), nameof(draft));
                MatchCallbackDispatcher = draft.MatchCallbackDispatcher ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.MatchCallbackDispatcher)), nameof(draft));
                LobbySubscriptionOperations = draft.LobbySubscriptionOperations ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.LobbySubscriptionOperations)), nameof(draft));
                TournamentSubscriptionStore = draft.TournamentSubscriptionStore ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.TournamentSubscriptionStore)), nameof(draft));
                TournamentCallbackDispatcher = draft.TournamentCallbackDispatcher ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.TournamentCallbackDispatcher)), nameof(draft));
                TournamentSubscriptionOperations = draft.TournamentSubscriptionOperations ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.TournamentSubscriptionOperations)), nameof(draft));
                TournamentLobbyLogic = draft.TournamentLobbyLogic ?? throw new ArgumentException(string.Format(ERROR_MISSING_DEPENDENCY_FORMAT, nameof(draft.TournamentLobbyLogic)), nameof(draft));
            }

            public IGuessWhoUnitOfWorkFactory UnitOfWorkFactory { get; }
            public UserSecuritySettings SecuritySettings { get; }
            public IPasswordHasher PasswordHasher { get; }
            public IVerificationCodeService VerificationCodeService { get; }
            public IEmailSender EmailSender { get; }
            public VerificationCodeEmailBuilder VerificationCodeEmailBuilder { get; }
            public MatchInvitationEmailBuilder MatchInvitationEmailBuilder { get; }
            public ILobbySubscriptionStore LobbySubscriptionStore { get; }
            public IMatchCallbackDispatcher MatchCallbackDispatcher { get; }
            public ILobbySubscriptionOperations LobbySubscriptionOperations { get; }
            public ITournamentSubscriptionStore TournamentSubscriptionStore { get; }
            public ITournamentCallbackDispatcher TournamentCallbackDispatcher { get; }
            public ITournamentSubscriptionOperations TournamentSubscriptionOperations { get; }
            public TournamentLobbyLogic TournamentLobbyLogic { get; }
        }
    }
}