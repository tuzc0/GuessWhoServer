using System.Data.Entity;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public class MatchData
    {
        public MATCH CreateMatch(CreateMatchArgs args)
        {
            byte statusDefault = 1;

            using (var dataBaseContext = new GuessWhoDB())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var match = new MATCH
                {
                    VISIBILITYID = args.Visibility,
                    STATUSID = statusDefault,
                    MODE = args.Mode,
                    MATCHCODE = args.MatchCode,
                    CREATEDATUTC = args.CreateDate,
                    STARTTIME = null,
                    ENDTIME = null,
                    WINNERUSERID = null
                };

                dataBaseContext.MATCH.Add(match);
                dataBaseContext.SaveChanges();

                var matchPlayer = new MATCH_PLAYER
                {
                    MATCHID = match.MATCHID,
                    USERID = args.UserProfileId,
                    SLOTNUMBER = 1,
                    ISWINNER = false,
                    JOINEDATUTC = args.CreateDate,
                    LEFTATUTC = null,
                    ISHOST = true,
                    ISREADY = false
                };

                dataBaseContext.MATCH_PLAYER.Add(matchPlayer);
                dataBaseContext.SaveChanges();
                transaction.Commit();
                return match;
            }
        }

        public bool MatchExists(string matchCode)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                return dataBaseContext.MATCH.Any(m => m.MATCHCODE == matchCode);
            }
        }
    }
}