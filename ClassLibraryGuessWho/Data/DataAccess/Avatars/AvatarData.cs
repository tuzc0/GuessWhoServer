
using System.Collections.Generic;
using System.Linq;
using System;
using GuessWhoServerDomain.Domain.Models.Avatars;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;

namespace GuessWhoDataAccess.Data.DataAccess.Avatars
{
    public class AvatarData : IAvatarRepository
    {
        private const string EMPTY = "";
     
        private readonly GuessWhoDBEntities dataBaseContext; 

        public AvatarData (GuessWhoDBEntities dataBaseContext)
        {
            this.dataBaseContext = dataBaseContext ??
                throw new ArgumentNullException(nameof(dataBaseContext));
        }

        public List<AvatarRecord> GetActiveAvatars()
        {
            return dataBaseContext.AVATAR
                    .Where(a => a.ISACTIVE)
                    .Select(a => new AvatarRecord
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
            string defaultAvatarId = dataBaseContext.AVATAR
                .Where(a => a.ISDEFAULT && a.ISACTIVE)
                .Select(a => a.AVATARID)
                .FirstOrDefault();

            return defaultAvatarId ?? string.Empty;
        }

        public bool AvatarExists(string avatarId)
        {
            string safeAvatarId = (avatarId ?? EMPTY).Trim();

            if (string.IsNullOrWhiteSpace(safeAvatarId))
            {
                return false;
            }

            return dataBaseContext.AVATAR.Any(a => a.AVATARID == safeAvatarId && a.ISACTIVE);
        }
    }
}
