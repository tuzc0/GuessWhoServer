using System;
using System.ServiceModel;
using GuessWho.Services.WCF.Services;

namespace ConsoleGuessWho
{
    internal static class Program
    {
        private static void Main()
        {
            using (var host = new ServiceHost(typeof(UserService)))
            {
                try
                {
                    host.Open();

                    Console.WriteLine("[UserService] Host abierto. Endpoints:");
                    foreach (var ep in host.Description.Endpoints)
                        Console.WriteLine($"  {ep.Address.Uri} [{ep.Binding.Name}] -> {ep.Contract.ContractType.FullName}");

                    Console.WriteLine("Presiona ENTER para detener el servicio...");
                    Console.ReadLine();

                    host.Close();
                }
                catch (AddressAlreadyInUseException)
                {
                    Console.Error.WriteLine("El puerto ya está en uso (8095). Cierra el proceso que lo ocupa o cambia el puerto en ambos configs.");
                }
                catch (CommunicationException ex)
                {
                    Console.Error.WriteLine("Fallo de comunicación al abrir el host: " + ex.Message);
                    host.Abort();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error inesperado: " + ex.Message);
                    host.Abort();
                }
            }
        }
    }
}
