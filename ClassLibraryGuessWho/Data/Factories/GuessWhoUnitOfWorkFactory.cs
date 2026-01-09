using GuessWhoDataAccess.Data.DataAccess.Accounts;
using GuessWhoDataAccess.Data.DataAccess.Avatars;
using GuessWhoDataAccess.Data.DataAccess.Characters;
using GuessWhoDataAccess.Data.DataAccess.Chat;
using GuessWhoDataAccess.Data.DataAccess.EmailVerification;
using GuessWhoDataAccess.Data.DataAccess.Friends;
using GuessWhoDataAccess.Data.DataAccess.Leaderboard;
using GuessWhoDataAccess.Data.DataAccess.Matches;
using GuessWhoDataAccess.Data.DataAccess.Profile;
using GuessWhoDataAccess.Data.DataAccess.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;

namespace GuessWhoDataAccess.Data.Factories
{
    public sealed class GuessWhoUnitOfWorkFactory : IGuessWhoUnitOfWorkFactory
    {
        private readonly IGuessWhoDbContextFactory contextFactory;

        public GuessWhoUnitOfWorkFactory(IGuessWhoDbContextFactory contextFactory)
        {
            this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public IGuessWhoUnitOfWork Create()
        {
            GuessWhoDBEntities context = contextFactory.Create();

            IUserAccountRepository userAccounts = new UserAccountData(context);
            IUserProfileRepository userProfiles = new UserProfileData(context);
            IEmailVerificationRepository emailVerification = new EmailVerificationData(context);
            IAvatarRepository avatars = new AvatarData(context);
            ICharacterRepository characters = new CharacterData(context);
            IMatchDeckRepository matchDecks = new CharacterDeckData(context);
            IFriendshipRepository friendships = new FriendshipData(context);
            IMatchRepository matches = new MatchData(context);
            IMatchInvitationRepository matchInvitations = new MatchInvitationData(context);
            IMatchChatRepository matchChat = new MatchChatData(context);
            IMatchTurnRepository matchTurns = new MatchTurnData(context);
            IMatchChessClockRepository matchChessClock = new MatchChessClockData(context);
            IMatchTurnAdvanceRepository matchTurnAdvance = new MatchTurnAdvanceData(context);
            IMatchGuessingRepository matchGuessing = new MatchGuessingData(context);
            ILeaderboardRepository leaderboards = new LeaderboardData(context);

            return new GuessWhoUnitOfWork(
                context,
                userAccounts,
                userProfiles,
                emailVerification,
                avatars,
                characters,
                matchDecks,
                friendships,
                matches,
                matchInvitations,
                matchChat,
                matchTurns,
                matchChessClock,
                matchTurnAdvance,
                matchGuessing,
                leaderboards);
        }
    }
}