using ClassLibraryGuessWho.Data.DataAccess.Characters;
using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.Factories;
using ClassLibraryGuessWho.Repositories.Implementation;
using ConsoleGuessWho.Infraestructure.Wcf;
using GuessWho.Services.WCF.Security;
using GuessWho.Services.WCF.Services;
using GuessWho.Services.WCF.Services.MatchApplication;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using log4net;
using log4net.Config;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Communication.Email.Builders;
using WcfServiceLibraryGuessWho.Coordinators;
using WcfServiceLibraryGuessWho.Coordinators.EmailVerification;
using WcfServiceLibraryGuessWho.Services.MatchApplication;
using WcfServiceLibraryGuessWho.Services.Settings;

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
            Logger.Info(SERVICE_HOST_STARTING_MESSAGE);

            try
            {
                var contextFactory = new GuessWhoDbContextFactory();
                var unitOfWorkFactory = new GuessWhoUnitOfWorkFactory(contextFactory);

                UserSecuritySettings securitySettings = UserSecuritySettingsLoader.Load();
                var verificationCodeService = new VerificationCodeService();

                Func<UserService> userServiceFactory = () =>
                {
                    var accountRepo = new UserAccountRepository(contextFactory);
                    var emailRepo = new EmailVerificationRepository(contextFactory);
                    var avatarRepo = new AvatarRepository(contextFactory);

                    IPasswordHasher passwordHasher = new Sha256PasswordHasher();

                    var smtpSettings = SmtpSettingsLoader.Load();
                    IEmailSender emailSender = new SmtpEmailSender(smtpSettings);

                    var verificationCodeEmailBuilder = new VerificationCodeEmailBuilder();

                    TimeSpan verificationCodeLifeTime = securitySettings.VerificationCodeLifetime;

                    ILog domainLogger = LogManager.GetLogger(typeof(EmailVerificationDomainService));
                    var emailVerificationDomainService = new EmailVerificationDomainService(
                        emailRepo,
                        verificationCodeService,
                        securitySettings,
                        domainLogger);

                    var registrationManager = new UserRegistrationManager(
                        unitOfWorkFactory,
                        avatarRepo,
                        emailSender,
                        verificationCodeEmailBuilder,
                        passwordHasher,
                        verificationCodeService,
                        verificationCodeLifeTime);

                    var emailVerificationManager = new EmailVerificationManager(
                        accountRepo,
                        emailRepo,
                        verificationCodeService,
                        emailSender,
                        verificationCodeEmailBuilder,
                        securitySettings,
                        emailVerificationDomainService);

                    var passwordRecoveryManager = new PasswordRecoveryManager(
                        accountRepo,
                        emailRepo,
                        emailSender,
                        verificationCodeEmailBuilder,
                        verificationCodeService,
                        emailVerificationDomainService,
                        passwordHasher,
                        securitySettings);

                    return new UserService(
                        registrationManager,
                        emailVerificationManager,
                        passwordRecoveryManager);
                };

                Func<LoginService> loginServiceFactory = () =>
                {
                    var accountRepo = new UserAccountRepository(contextFactory);
                    var sessionRepo = new GameSessionRepository(contextFactory);

                    var loginManager = new LoginManager(accountRepo);
                    var sessionManager = new GameSessionManager(sessionRepo);

                    var loginCoordinator = new LoginCoordinator(loginManager, sessionManager, accountRepo);

                    return new LoginService(loginCoordinator);
                };

                Func<MatchService> matchServiceFactory = () =>
                {
                    var matchData = new MatchData(contextFactory.Create());
                    var lobbyNotifier = new LobbyNotifier(LogManager.GetLogger(typeof(LobbyNotifier)));
                    var deckProvider = new MatchDeckProvider(new CharacterData(), new CharacterDeckData());

                    return new MatchService(
                        new MatchCreationService(matchData),
                        new LobbyCoordinator(matchData, lobbyNotifier),
                        new MatchLifecycleService(matchData, lobbyNotifier, deckProvider));
                };

                using (ServiceHost hostUser = new ServiceHost(typeof(UserService)))
                using (ServiceHost hostLogin = new ServiceHost(typeof(LoginService)))
                using (ServiceHost hostMatch = new ServiceHost(typeof(MatchService)))
                {
                    hostUser.Description.Behaviors.Add(new DelegateServiceBehavior(() => userServiceFactory()));
                    hostLogin.Description.Behaviors.Add(new DelegateServiceBehavior(() => loginServiceFactory()));
                    hostMatch.Description.Behaviors.Add(new DelegateServiceBehavior(() => matchServiceFactory()));

                    hostUser.Open();
                    hostLogin.Open();
                    hostMatch.Open();

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
    }
}
