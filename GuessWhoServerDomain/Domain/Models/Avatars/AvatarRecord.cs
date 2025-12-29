namespace GuessWhoServerDomain.Domain.Models.Avatars
{
    public sealed class AvatarRecord
    {
        public string AvatarId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(AvatarId);

        public static AvatarRecord CreateInvalid()
        {
            return new AvatarRecord
            {
                AvatarId = string.Empty,
                Name = string.Empty,
                IsDefault = false,
                IsActive = false
            };
        }
    }
}
