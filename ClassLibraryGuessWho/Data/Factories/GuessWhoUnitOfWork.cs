using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;
using System.Data.Entity;

namespace GuessWhoDataAccess.Data.Factories
{
    public sealed class GuessWhoUnitOfWork : IGuessWhoUnitOfWork
    {
        private readonly GuessWhoDBEntities context;

        public GuessWhoUnitOfWork(
            GuessWhoDBEntities context,
            IUserAccountRepository userAccounts,
            IUserProfileRepository userProfiles,
            IEmailVerificationRepository emailVerification,
            IAvatarRepository avatars,
            ICharacterRepository characters,
            IMatchDeckRepository matchDecks,
            IFriendshipRepository friendships,
            IMatchRepository matches,
            IMatchInvitationRepository matchInvitations,
            IMatchChatRepository matchChats,
            IMatchTurnRepository matchTurns,
            IMatchChessClockRepository matchChessClocks,
            IMatchTurnAdvanceRepository matchTurnAdvance,
            IMatchGuessingRepository matchGuessing,
            ILeaderboardRepository leaderboards,
            ITournamentRepository tournaments)
        {
            this.context = context ??
                throw new ArgumentNullException(nameof(context));

            UserAccounts = userAccounts ??
                throw new ArgumentNullException(nameof(userAccounts));
            UserProfiles = userProfiles ??
                throw new ArgumentNullException(nameof(userProfiles));
            EmailVerification = emailVerification ??
                throw new ArgumentNullException(nameof(emailVerification));
            Avatars = avatars ??
                throw new ArgumentNullException(nameof(avatars));
            Characters = characters ??
                throw new ArgumentNullException(nameof(characters));
            MatchDecks = matchDecks ??
                throw new ArgumentNullException(nameof(matchDecks));
            Friendships = friendships ??
                throw new ArgumentNullException(nameof(friendships));
            Matches = matches ??
                throw new ArgumentNullException(nameof(matches));
            MatchInvitations = matchInvitations ??
                throw new ArgumentNullException(nameof(matchInvitations));
            MatchesChats = matchChats ??
                throw new ArgumentNullException(nameof(matchChats));
            MatchesTurns = matchTurns ??
                throw new ArgumentNullException(nameof(matchTurns));
            MatchChessClocks = matchChessClocks ??
                throw new ArgumentNullException(nameof(matchChessClocks));
            MatchTurnAdvances = matchTurnAdvance ??
                throw new ArgumentNullException(nameof(matchTurnAdvance));
            MatchGuessing = matchGuessing ??
                throw new ArgumentNullException(nameof(matchGuessing));
            Leaderboards = leaderboards ??
                throw new ArgumentNullException(nameof(leaderboards));
            Tournaments = tournaments ??
                throw new ArgumentNullException(nameof(tournaments));
        }

        public GuessWhoDBEntities Context => context;

        public IUserAccountRepository UserAccounts { get; }
        public IUserProfileRepository UserProfiles { get; }
        public IEmailVerificationRepository EmailVerification { get; }
        public IAvatarRepository Avatars { get; }
        public ICharacterRepository Characters { get; }
        public IMatchDeckRepository MatchDecks { get; }
        public IFriendshipRepository Friendships { get; }
        public IMatchRepository Matches { get; }
        public IMatchInvitationRepository MatchInvitations { get; }
        public IMatchChatRepository MatchesChats { get; }
        public IMatchTurnRepository MatchesTurns { get; }
        public IMatchChessClockRepository MatchChessClocks { get; }
        public IMatchTurnAdvanceRepository MatchTurnAdvances { get; }
        public IMatchGuessingRepository MatchGuessing { get; }
        public ILeaderboardRepository Leaderboards { get; }
        public ITournamentRepository Tournaments { get; }

        public IGuessWhoDbTransaction BeginTransaction()
        {
            DbContextTransaction efTransaction = context.Database.BeginTransaction();
            return new EfDbTransaction(efTransaction);
        }

        public void Flush()
        {
            context.SaveChanges();
        }

        public void Dispose()
        {
            context.Dispose();
        }

        private sealed class EfDbTransaction : IGuessWhoDbTransaction
        {
            private readonly DbContextTransaction transaction;

            public EfDbTransaction(DbContextTransaction transaction)
            {
                this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            }

            public void Commit()
            {
                transaction.Commit();
            }

            public void Rollback()
            {
                transaction.Rollback();
            }

            public void Dispose()
            {
                transaction.Dispose();
            }
        }
    }
}