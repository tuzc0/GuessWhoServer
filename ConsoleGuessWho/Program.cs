using System;
using System.ServiceModel;
using GuessWho.Services.WCF.Services;

namespace ConsoleGuessWho
{
    internal class Program
    {
        private static void Main()
        {
            using (var host = new ServiceHost(typeof(UserService)))
            {
                host.Open();

                foreach (var ep in host.Description.Endpoints)
                {
                    Console.WriteLine($"  {ep.Address.Uri} [{ep.Binding.Name}] -> {ep.Contract.ContractType.FullName}");
                }

                Console.ReadLine();
            }
        }
    }
}
