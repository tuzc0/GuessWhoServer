using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.Avatars;
using ClassLibraryGuessWho.Data.Factories;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoServices.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Repositories.Implementation
{
    public class AvatarRepository : IAvatarRepository
    {
        private readonly IGuessWhoDbContextFactory contextFactory;
        public AvatarRepository(IGuessWhoDbContextFactory contextFactory)
        {
            this.contextFactory = contextFactory ??
                throw new ArgumentNullException(nameof(contextFactory));
        }

        private T Excute<T> (Func<IAvatarData, T> action)
        {
            using (var context = contextFactory.Create())
            {
                var avatarData = new AvatarData(context);
                return action(avatarData);
            }
        }

        public List<AvatarDto> GetActiveAvatars() => 
            Excute(avatarData => avatarData.GetActiveAvatars());

        public string GetDefaultAvatarId() => 
            Excute(avatarData => avatarData.GetDefaultAvatarId());
    }
}
