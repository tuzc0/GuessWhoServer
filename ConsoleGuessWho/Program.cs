using System;
using System.Configuration;
using System.ServiceModel;
using GuessWho.Services.WCF.Services;
using WcfServiceLibraryGuessWho.Services;

namespace ConsoleGuessWho
{
    internal static class Program
    {
        private static void Main()
        {
            using (var hostUser = new ServiceHost(typeof(UserService)))
            using (var hostLogin = new ServiceHost(typeof(LoginService)))
            using (var hostChat = new ServiceHost(typeof(ChatService)))
            using (var hostFriendRequest = new ServiceHost(typeof(FriendService)))
            {
                try
                {
                    hostUser.Open();
                    hostLogin.Open();
                    hostChat.Open();
                    hostFriendRequest.Open();

                    // Mostrar información de UserService
                    Console.WriteLine("[UserService] Host abierto. Endpoints:");
                    foreach (var ep in hostUser.Description.Endpoints)
                        Console.WriteLine($"  {ep.Address.Uri} [{ep.Binding.Name}] -> {ep.Contract.ContractType.FullName}");

                    // Mostrar información de LoginService
                    Console.WriteLine("\n[LoginService] Host abierto. Endpoints:");
                    foreach (var ep in hostLogin.Description.Endpoints)
                        Console.WriteLine($"  {ep.Address.Uri} [{ep.Binding.Name}] -> {ep.Contract.ContractType.FullName}");

                    // Mostrar información de ChatService
                    Console.WriteLine("\n[ChatService] Host abierto. Endpoints:");
                    foreach (var ep in hostChat.Description.Endpoints)
                        Console.WriteLine($"  {ep.Address.Uri} [{ep.Binding.Name}] -> {ep.Contract.ContractType.FullName}");

                    Console.WriteLine("[CFG] " + AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

                    var cs = ConfigurationManager.ConnectionStrings["GuessWhoDB"];
                    Console.WriteLine(cs != null ? "Conn OK" : "Conn MISSING");

                    Console.WriteLine("[SMTP User] " + ConfigurationManager.AppSettings["Smtp.User"]);
                    Console.WriteLine("Presiona ENTER para detener el servicio...");
                    Console.ReadLine();

                    hostChat.Close();
                    hostFriendRequest.Close();
                    hostLogin.Close();
                    hostUser.Close();
                }
                catch (AddressAlreadyInUseException ex)
                {
                    Console.Error.WriteLine("El puerto ya está en uso. Cierra el proceso que lo ocupa o cambia el puerto en App.config.\n" + ex.Message);
                }
                catch (CommunicationException ex)
                {
                    Console.Error.WriteLine("Fallo de comunicación al abrir el host: " + ex.Message);
                    hostUser.Abort();
                    hostLogin.Abort();
                    hostChat.Abort();
                    hostFriendRequest.Abort();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error inesperado: " + ex.Message);
                    hostUser.Abort();
                    hostLogin.Abort();
                    hostFriendRequest.Abort();
                    hostChat.Abort();
                }
            }
        }
    }
}
