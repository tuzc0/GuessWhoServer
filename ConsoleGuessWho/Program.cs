using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.Factories;
using ConsoleGuessWho.Infraestructure.Wcf;
using GuessWho.Services.WCF.Services;
using GuessWhoServices.Repositories.Implementation;
using GuessWhoServices.Repositories.Interfaces;
using log4net;
using log4net.Config;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Coordinators;
using WcfServiceLibraryGuessWho.Coordinators.EmailVerification;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;
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
        private const string SERVICE_HOST_ERROR_FAILED_COMMUNICATION = "Failed to establish communication when openning the host";

        private static void Main()
        {
            Logger.Info(SERVICE_HOST_STARTING_MESSAGE);

            var userContext = new GuessWhoDBEntities();
            var loginContext = new GuessWhoDBEntities();
            var profileContext = new GuessWhoDBEntities();

            try
            {
                var contextFactory = new GuessWhoDbContextFactory();

                Func<UserService> userServiceFactory = () =>
                {
                    IUserAccountRepository accountRepository = new UserAccountRepository(contextFactory);
                    IEmailVerificationRepository emailVerificationRepository = new EmailVerificationRepository(contextFactory);
                    IAvatarRepository avatarRepository = new AvatarRepository(contextFactory);

                    UserSecuritySettings userSecuritySettings = UserSecuritySettingsLoader.Load();

                    IVerificationEmailSender verificationEmailSender = new VerificationEmailSender();

                    IVerificationCodeService verificationCodeService = new VerificationCodeService();
                    IVerificationEmailDispatcher verificationEmailDispatcher =
                        new VerificationEmailDispatcher(verificationEmailSender);

                    IUserRegistrationManager userRegistrationManager =
                        new UserRegistrationManager(
                            accountRepository,
                            emailVerificationRepository,
                            avatarRepository,
                            verificationEmailSender);

                    IEmailVerificationManager emailVerificationManager =
                        new EmailVerificationManager(
                            accountRepository,
                            emailVerificationRepository,
                            verificationCodeService,
                            verificationEmailDispatcher,
                            userSecuritySettings);

                    IPasswordRecoveryManager passwordRecoveryManager =
                        new PasswordRecoveryManager(
                            accountRepository,
                            emailVerificationRepository,
                            verificationCodeService,
                            verificationEmailDispatcher,
                            userSecuritySettings);

                    IUserFaultMapper userFaultMapper = new UserFaultMapper();

                    return new UserService(
                        userRegistrationManager,
                        emailVerificationManager,
                        passwordRecoveryManager,
                        userFaultMapper);
                };

                var loginService = new LoginService(new UserAccountData(loginContext));
                var updateProfileService = new UpdateProfileService(new UserAccountData(profileContext));

                using (var hostUser = new ServiceHost(typeof(UserService)))
                using (var hostLogin = new ServiceHost(loginService))
                using (var hostUpdateProfile = new ServiceHost(updateProfileService))
                using (var hostChat = new ServiceHost(typeof(ChatService)))
                using (var hostFriendRequest = new ServiceHost(typeof(FriendService)))
                using (var hostMatch = new ServiceHost(typeof(MatchService)))
                {
                    hostUser.Description.Behaviors.Add(new DelegateServiceBehavior(() => userServiceFactory()));
                    hostUser.Open();
                    hostLogin.Open();
                    hostUpdateProfile.Open();

                    hostChat.Open();
                    hostFriendRequest.Open();
                    hostMatch.Open();

                    Logger.Info(SERVICE_HOST_STARTED_MESSAGE);

                    Console.ReadLine();

                    Logger.Info(SERVICE_HOST_STOPPING_MESSAGE);

                    hostMatch.Close();
                    hostFriendRequest.Close();
                    hostChat.Close();

                    hostUpdateProfile.Close();
                    hostLogin.Close();
                    hostUser.Close();

                    Logger.Info(SERVICE_HOST_STOPPED_MESSAGE);
                }
            }
            catch (AddressAlreadyInUseException ex)
            {
                Logger.Error(SERVICE_HOST_ERROR_ADDRESS_IN_USE, ex);
            }
            catch (CommunicationException ex)
            {
                Logger.Error(SERVICE_HOST_ERROR_FAILED_COMMUNICATION, ex);
            }
            catch (Exception ex)
            {
                Logger.Fatal(SERVICE_HOST_FATAL_ERROR_MESSAGE, ex);
            }
            finally
            {
                userContext.Dispose();
                loginContext.Dispose();
                profileContext.Dispose();
            }
        }
    }
}
