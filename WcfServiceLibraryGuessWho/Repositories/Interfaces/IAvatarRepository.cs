using GuessWhoContracts.Dtos.Dto;
using System.Collections.Generic;

namespace GuessWhoServices.Repositories.Interfaces
{
    public interface IAvatarRepository
    {
        List<AvatarDto> GetActiveAvatars();

        string GetDefaultAvatarId();
    }
}
