namespace ClassLibraryGuessWho.Data.Factories
{
    public class GuessWhoDbContextFactory : IGuessWhoDbContextFactory
    {
        public GuessWhoDBEntities Create()
        {
            return new GuessWhoDBEntities();
        }
    }
}
