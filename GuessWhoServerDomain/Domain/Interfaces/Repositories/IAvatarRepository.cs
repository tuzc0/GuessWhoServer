using GuessWhoServerDomain.Domain.Models.Avatars;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IAvatarRepository
    {
        List<AvatarRecord> GetActiveAvatars();

        string GetDefaultAvatarId();
    }
}
