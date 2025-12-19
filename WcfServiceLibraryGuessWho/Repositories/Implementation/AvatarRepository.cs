using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.Avatars;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoServices.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Repositories.Implementation
{
    public class AvatarRepository : IAvatarRepository
    {
        private readonly AvatarData avatarData; 
        public AvatarRepository(AvatarData avatarData)
        {
            this.avatarData = avatarData ??
                throw new ArgumentNullException(nameof(avatarData));
        }

        public AvatarRepository(GuessWhoDBEntities dataContext) : 
            this( new AvatarData(dataContext))
        {
        }

        public List<AvatarDto> GetActiveAvatars()
        {
            return avatarData.GetActiveAvatars();
        }

        public string GetDefaultAvatarId()
        {
            return avatarData.GetDefaultAvatarId();
        }
    }
}
