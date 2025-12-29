namespace GuessWhoServerDomain.Domain.Enums.Security
{
    public sealed class VerificationCodeResult
    {
        public VerificationCodeResult(string plainCode, byte[] hashCode) 
        {
            PlainCode = plainCode ?? string.Empty; 
            HashCode = hashCode ?? System.Array.Empty<byte>();
        }

        public string PlainCode { get; }
        public byte[] HashCode { get; }
    }
}
