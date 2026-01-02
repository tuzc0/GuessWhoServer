using GuessWhoDataAccess.Data.Factories;

namespace GuessWhoDataAccess.Data.Factories
{
    public class GuessWhoDbContextFactory : IGuessWhoDbContextFactory
    {
        public GuessWhoDBEntities Create()
        {
            return new GuessWhoDBEntities();
        }
    }
}
