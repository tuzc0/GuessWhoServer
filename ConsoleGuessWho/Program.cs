using System;
using System.ServiceModel;
using GuessWho.Services.WCF.Services;
using log4net;
using log4net.Config;

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

            using (var hostUser = new ServiceHost(typeof(UserService)))
            using (var hostLogin = new ServiceHost(typeof(LoginService)))
            using (var hostChat = new ServiceHost(typeof(ChatService)))
            using (var hostFriendRequest = new ServiceHost(typeof(FriendService)))
            using (var hostMatch = new ServiceHost(typeof(MatchService)))
            using (var hostUpdateProfile = new ServiceHost(typeof(UpdateProfileService)))
            {
                try
                {
                    hostUser.Open();
                    hostLogin.Open();
                    hostChat.Open();
                    hostFriendRequest.Open();
                    hostMatch.Open();
                    hostUpdateProfile.Open();

                    Logger.Info(SERVICE_HOST_STARTED_MESSAGE);
                    
                    Console.ReadLine();

                    Logger.Info(SERVICE_HOST_STOPPING_MESSAGE);
                    
                    hostChat.Close();
                    hostFriendRequest.Close();
                    hostLogin.Close();
                    hostUser.Close();
                    hostMatch.Close();
                    hostUpdateProfile.Close();
                    
                    Logger.Info(SERVICE_HOST_STOPPED_MESSAGE);
                }
                catch (AddressAlreadyInUseException ex)
                {
                    Logger.Error(SERVICE_HOST_ERROR_ADDRESS_IN_USE, ex);
                }
                catch (CommunicationException ex)
                {
                    Logger.Error(SERVICE_HOST_ERROR_FAILED_COMMUNICATION, ex);
                    hostUser.Abort();
                    hostLogin.Abort();
                    hostChat.Abort();
                    hostFriendRequest.Abort();
                    hostMatch.Abort();
                    hostUpdateProfile.Abort();
                }
                catch (Exception ex)
                {
                    Logger.Fatal(SERVICE_HOST_FATAL_ERROR_MESSAGE, ex);
                    hostUser.Abort();
                    hostLogin.Abort();
                    hostFriendRequest.Abort();
                    hostChat.Abort();
                    hostMatch.Abort();
                    hostUpdateProfile.Abort();
                }
            }
        }
    }
}
