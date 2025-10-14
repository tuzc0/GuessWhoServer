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
            {
                try
                {
                    hostUser.Open();
                    hostLogin.Open();

                    Console.WriteLine("[UserService] Host abierto. Endpoints:");
                    foreach (var ep in hostUser.Description.Endpoints)
                        Console.WriteLine($"  {ep.Address.Uri} [{ep.Binding.Name}] -> {ep.Contract.ContractType.FullName}");

                    Console.WriteLine("\n[LoginService] Host abierto. Endpoints:");
                    foreach (var ep in hostLogin.Description.Endpoints)
                        Console.WriteLine($"  {ep.Address.Uri} [{ep.Binding.Name}] -> {ep.Contract.ContractType.FullName}");

                    Console.WriteLine("\nPresiona ENTER para detener los servicios...");
                    Console.ReadLine();

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
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error inesperado: " + ex.Message);
                    hostUser.Abort();
                    hostLogin.Abort();
                }
            }
        }
    }
}
