using System;

namespace GuessWhoServerDomain.Domain.Parameters.Accounts
{
    public sealed class UpdateProfileArgs
    {
        public long UserId { get; }
        public string NewDisplayName { get; }
        public string CurrentPasswordPlain { get; }
        public string NewPasswordPlain { get; }
        public string NewAvatarId { get; }
        public DateTime NowUtc { get; }

        public UpdateProfileArgs(long userId, string newDisplayName, string currentPasswordPlain,
            string newPasswordPlain, string newAvatarId, DateTime nowUtc)
        {
            UserId = userId;
            NewDisplayName = newDisplayName ?? string.Empty;
            CurrentPasswordPlain = currentPasswordPlain ?? string.Empty;
            NewPasswordPlain = newPasswordPlain ?? string.Empty;
            NewAvatarId = newAvatarId ?? string.Empty;
            NowUtc = nowUtc;
        }
    }
}
