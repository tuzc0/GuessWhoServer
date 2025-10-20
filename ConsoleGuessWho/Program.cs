using System;
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
            using (var hostChat = new ServiceHost(typeof(ChatService))) // <-- Host para ChatService
            {
                try
                {
                    // Abrir todos los hosts
                    hostUser.Open();
                    hostLogin.Open();
                    hostChat.Open();

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

                    Console.WriteLine("\nPresiona ENTER para detener los servicios...");
                    Console.ReadLine();

                    // Cerrar hosts
                    hostChat.Close();
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
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error inesperado: " + ex.Message);
                    hostUser.Abort();
                    hostLogin.Abort();
                    hostChat.Abort();
                }
            }
        }
    }
}
