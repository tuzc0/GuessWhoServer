using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Characters;
using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.Factories;
using ConsoleGuessWho.Infraestructure.Wcf;
using GuessWho.Services.WCF.Services;
using GuessWho.Services.WCF.Services.MatchApplication;
using GuessWhoServices.Repositories.Implementation;
using GuessWhoServices.Repositories.Interfaces;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Coordinators;
using WcfServiceLibraryGuessWho.Coordinators.EmailVerification;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs.Mappers;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;
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
        private const string SERVICE_HOST_ERROR_ADDRESS_IN_USE = "The port is already in use";
        private const string SERVICE_HOST_ERROR_FAILED_COMMUNICATION = "Failed to establish communication";

        static void Main(string[] args)
        {
            Logger.Info(SERVICE_HOST_STARTING_MESSAGE);

            try
            {
                var contextFactory = new GuessWhoDbContextFactory();

                Func<UserService> userServiceFactory = () => {
                    var accountRepo = new UserAccountRepository(contextFactory);
                    var emailRepo = new EmailVerificationRepository(contextFactory);
                    var avatarRepo = new AvatarRepository(contextFactory);
                    var settings = UserSecuritySettingsLoader.Load();
                    var emailSender = new VerificationEmailSender();
                    var codeService = new VerificationCodeService();
                    var dispatcher = new VerificationEmailDispatcher(emailSender);

                    return new UserService(
                        new UserRegistrationManager(accountRepo, emailRepo, avatarRepo, emailSender),
                        new EmailVerificationManager(accountRepo, emailRepo, codeService, dispatcher, settings),
                        new PasswordRecoveryManager(accountRepo, emailRepo, codeService, dispatcher, settings),
                        new UserFaultMapper()
                    );
                };

                Func<LoginService> loginServiceFactory = () => {
                    var accountRepo = new UserAccountRepository(contextFactory);
                    var sessionRepo = new GameSessionRepository(contextFactory);
                    return new LoginService(
                        new LoginManager(accountRepo, sessionRepo),
                        new LoginFaultMapper()
                    );
                };

                Func<MatchService> matchServiceFactory = () => {
                    var matchData = new MatchData(contextFactory.Create());
                    var lobbyNotifier = new LobbyNotifier(LogManager.GetLogger(typeof(LobbyNotifier)));
                    var deckProvider = new MatchDeckProvider(new CharacterData(), new CharacterDeckData());

                    return new MatchService(
                        new MatchCreationService(matchData),
                        new LobbyCoordinator(matchData, lobbyNotifier),
                        new MatchLifecycleService(matchData, lobbyNotifier, deckProvider)
                    );
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
                    Console.WriteLine("Servidor en línea. Presiona ENTER para cerrar.");
                    Console.ReadLine();

                    Logger.Info(SERVICE_HOST_STOPPING_MESSAGE);
                }

                Logger.Info(SERVICE_HOST_STOPPED_MESSAGE);
            }
            catch (Exception ex)
            {
                Logger.Fatal(SERVICE_HOST_FATAL_ERROR_MESSAGE, ex);
                Console.WriteLine("ERROR FATAL: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}