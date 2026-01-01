using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Avatars;
using ClassLibraryGuessWho.Data.DataAccess.Characters;
using ClassLibraryGuessWho.Data.DataAccess.Chat;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using ClassLibraryGuessWho.Data.DataAccess.Friends;
using ClassLibraryGuessWho.Data.DataAccess.Matches;
using ClassLibraryGuessWho.Data.DataAccess.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;

namespace ClassLibraryGuessWho.Data.Factories
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
            IEmailVerificationRepository emailVerification = new EmailVerificationData(context);
            IAvatarRepository avatars = new AvatarData(context);
            ICharacterRepository characters = new CharacterData(context);
            IMatchDeckRepository matchDecks = new CharacterDeckData(context);
            IFriendshipRepository friendships = new FriendshipData(context);
            IMatchRepository matches = new MatchData(context);
            IMatchChatRepository matchChat = new MatchChatData(context);
            IMatchTurnRepository matchTurns = new MatchTurnData(context);
            IMatchChessClockRepository matchChessClock = new MatchChessClockData(context);
            IMatchTurnAdvanceRepository matchTurnAdvance = new MatchTurnAdvanceData(context);
            IMatchGuessingRepository matchGuessing = new MatchGuessingData(context);

            return new GuessWhoUnitOfWork(
                context,
                userAccounts,
                emailVerification,
                avatars,
                characters,
                matchDecks,
                friendships,
                matches,
                matchChat,
                matchTurns,
                matchChessClock,
                matchTurnAdvance,
                matchGuessing);
        }
    }
}
