using GuessWhoContracts.Dtos.Dto;
using System.Collections.Generic;

namespace ClassLibraryGuessWho.Data.DataAccess.Avatars
{
    public interface IAvatarData
    {
        List<AvatarDto> GetActiveAvatars();
        string GetDefaultAvatarId();
    }
}
