using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Matches
{
    public sealed partial class MatchData : IMatchRepository
    {
        private readonly GuessWhoDBEntities dataContext;

        public MatchData(GuessWhoDBEntities dataContext)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }
    }
}
