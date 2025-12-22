using GuessWhoContracts.Dtos.Dto;
using System.Collections.Generic;
using System.Linq;
using System; 

namespace ClassLibraryGuessWho.Data.DataAccess.Avatars
{
    public class AvatarData : IAvatarData
    {
        private readonly GuessWhoDBEntities dataBaseContext; 

        public AvatarData (GuessWhoDBEntities dataBaseContext)
        {
            this.dataBaseContext = dataBaseContext ??
                throw new ArgumentNullException(nameof(dataBaseContext));
        }

        public List<AvatarDto> GetActiveAvatars()
        {
            return dataBaseContext.AVATAR
                    .Where(a => a.ISACTIVE)
                    .Select(a => new AvatarDto
                    {
                        AvatarId = a.AVATARID,
                        Name = a.NAME,
                        IsDefault = a.ISDEFAULT,
                        IsActive = a.ISACTIVE
                    })
                    .ToList();
        }

        public string GetDefaultAvatarId()
        {
            var defaultAvatar = dataBaseContext.AVATAR
                    .FirstOrDefault(a => a.ISDEFAULT && a.ISACTIVE);

            return defaultAvatar?.AVATARID;
        }
    }
}
