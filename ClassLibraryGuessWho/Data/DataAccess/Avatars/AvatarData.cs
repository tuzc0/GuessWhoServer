using GuessWhoContracts.Dtos.Dto;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Avatars
{
    public class AvatarData
    {
        public List<AvatarDto> GetActiveAvatars()
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.AVATAR
                    .Where(a => a.ISACTIVE)
                    .Select(a => new AvatarDto
                    {
                        AvatarId = a.AVATARID,
                        Name = a.NAME,
                        isDefault = a.ISDEFAULT,
                        isActive = a.ISACTIVE
                    })
                    .ToList();
            } 
        }

        public string GetDefaultAvatarId()
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var defaultAvatar = dataBaseContext.AVATAR
                    .FirstOrDefault(a => a.ISDEFAULT && a.ISACTIVE);

                return defaultAvatar?.AVATARID;
            }
        }

    }
}
