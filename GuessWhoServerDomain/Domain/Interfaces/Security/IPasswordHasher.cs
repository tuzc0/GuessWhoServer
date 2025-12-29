namespace GuessWhoServerDomain.Domain.Interfaces.Security
{
    public interface IPasswordHasher
    {
        byte[] HashPassword(string password);

        bool VerifyPassword(string password, byte[] storedPasswordHashBytes);
    }
}
