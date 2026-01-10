using GuessWhoContracts.Services;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Turns;
using GuessWhoServices.Coordinators.Match;
using GuessWhoServices.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace GuessWhoTests.Services.Coordinators.Match
{
    [TestClass]
    public class MatchSecretCharacterLogicTests
    {
        private const long MATCH_ID = 800;
        private const long USER_ID = 101;
        private const long OPPONENT_ID = 102;
        private const string CHARACTER_ID = "A0001";

        private Mock<IGuessWhoUnitOfWorkFactory> factoryMock;
        private Mock<IGuessWhoUnitOfWork> uowMock;
        private Mock<IMatchRepository> matchRepoMock;
        private Mock<IMatchTurnRepository> turnRepoMock;
        private Mock<IMatchCallbackDispatcher> dispatcherMock;
        private Mock<IGuessWhoDbTransaction> transactionMock;
        private MatchSecretCharacterLogic logic;

        [TestInitialize]
        public void TestInitialize()
        {
            factoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            uowMock = new Mock<IGuessWhoUnitOfWork>();
            matchRepoMock = new Mock<IMatchRepository>();
            turnRepoMock = new Mock<IMatchTurnRepository>();
            dispatcherMock = new Mock<IMatchCallbackDispatcher>();
            transactionMock = new Mock<IGuessWhoDbTransaction>();

            factoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            uowMock.Setup(u => u.Matches).Returns(matchRepoMock.Object);
            uowMock.Setup(u => u.MatchesTurns).Returns(turnRepoMock.Object);
            uowMock.Setup(u => u.BeginTransaction()).Returns(transactionMock.Object);

            logic = new MatchSecretCharacterLogic(factoryMock.Object, dispatcherMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new MatchSecretCharacterLogic(null, dispatcherMock.Object);
        }

        [TestMethod]
        public void TestChooseSecretCharacter_InvalidMatchId_ShouldReturnMatchNotFound()
        {
            ChooseSecretCharacterArgs args = CreateChooseArgs(0, USER_ID, CHARACTER_ID);

            ChooseSecretCharacterResult result = logic.ChooseSecretCharacter(args);

            Assert.AreEqual(ChooseSecretCharacterResultCode.MatchNotFound, result.Code);
        }

        [TestMethod]
        public void TestChooseSecretCharacter_EmptyCharacter_ShouldReturnInvalidCharacter()
        {
            ChooseSecretCharacterArgs args = CreateChooseArgs(MATCH_ID, USER_ID, " ");

            ChooseSecretCharacterResult result = logic.ChooseSecretCharacter(args);

            Assert.AreEqual(ChooseSecretCharacterResultCode.InvalidCharacter, result.Code);
        }

        [TestMethod]
        public void TestChooseSecretCharacter_RepoFailure_ShouldRollback()
        {
            ChooseSecretCharacterArgs args = CreateChooseArgs(MATCH_ID, USER_ID, CHARACTER_ID);
            matchRepoMock.Setup(r => r.ChooseSecretCharacter(It.IsAny<ChooseSecretCharacterArgs>()))
                .Returns(ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.OperationConflict));

            logic.ChooseSecretCharacter(args);

            transactionMock.Verify(t => t.Rollback(), Times.Once());
        }

        [TestMethod]
        public void TestChooseSecretCharacter_AllChosenSuccess_ShouldBroadcastAllChosen()
        {
            ChooseSecretCharacterArgs args = CreateChooseArgs(MATCH_ID, USER_ID, CHARACTER_ID);
            matchRepoMock.Setup(r => r.ChooseSecretCharacter(It.IsAny<ChooseSecretCharacterArgs>()))
                .Returns(ChooseSecretCharacterResult.Success());
            matchRepoMock.Setup(r => r.AreAllSecretCharactersChosen(MATCH_ID)).Returns(true);
            SetupSuccessfulTurnInit();

            logic.ChooseSecretCharacter(args);

            dispatcherMock.Verify(d => d.Broadcast(MATCH_ID, It.IsAny<Action<IMatchCallback>>()), Times.Exactly(2));
        }

        [TestMethod]
        public void TestChangeSecretCharacter_InvalidUserId_ShouldReturnPlayerNotInMatch()
        {
            ChangeSecretCharacterArgs args = new ChangeSecretCharacterArgs(MATCH_ID, 0, CHARACTER_ID);

            ChangeSecretCharacterResult result = logic.ChangeSecretCharacter(args);

            Assert.AreEqual(ChangeSecretCharacterResultCode.PlayerNotInMatch, result.Code);
        }

        [TestMethod]
        public void TestChangeSecretCharacter_Success_ShouldCommitAndBroadcast()
        {
            ChangeSecretCharacterArgs args = new ChangeSecretCharacterArgs(MATCH_ID, USER_ID, CHARACTER_ID);
            matchRepoMock.Setup(r => r.ChangeSecretCharacter(It.IsAny<ChangeSecretCharacterArgs>()))
                .Returns(ChangeSecretCharacterResult.Success());

            logic.ChangeSecretCharacter(args);

            transactionMock.Verify(t => t.Commit(), Times.Once());
        }

        [TestMethod]
        public void TestTryInitializeFirstTurn_AlreadyInitialized_ShouldReturnSuccessPath()
        {
            ChooseSecretCharacterArgs args = CreateChooseArgs(MATCH_ID, USER_ID, CHARACTER_ID);
            matchRepoMock.Setup(r => r.AreAllSecretCharactersChosen(MATCH_ID)).Returns(true);
            matchRepoMock.Setup(r => r.ChooseSecretCharacter(It.IsAny<ChooseSecretCharacterArgs>()))
                .Returns(ChooseSecretCharacterResult.Success());
            TurnStateSnapshot state = new TurnStateSnapshot(MATCH_ID, 1, 0, USER_ID, DateTime.UtcNow);
            turnRepoMock.Setup(r => r.GetTurnState(MATCH_ID)).Returns(state);

            ChooseSecretCharacterResult result = logic.ChooseSecretCharacter(args);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestTryInitializeFirstTurn_NotEnoughPlayers_ShouldReturnFailedOutcome()
        {
            ChooseSecretCharacterArgs args = CreateChooseArgs(MATCH_ID, USER_ID, CHARACTER_ID);
            matchRepoMock.Setup(r => r.AreAllSecretCharactersChosen(MATCH_ID)).Returns(true);
            matchRepoMock.Setup(r => r.ChooseSecretCharacter(It.IsAny<ChooseSecretCharacterArgs>()))
                .Returns(ChooseSecretCharacterResult.Success());
            matchRepoMock.Setup(r => r.GetActivePlayerIds(MATCH_ID, 2)).Returns(new List<long> { USER_ID });

            ChooseSecretCharacterResult result = logic.ChooseSecretCharacter(args);

            Assert.AreEqual(ChooseSecretCharacterResultCode.OperationConflict, result.Code);
        }

        private ChooseSecretCharacterArgs CreateChooseArgs(long matchId, long userId, string characterId)
        {
            ChooseSecretCharacterArgs args = new ChooseSecretCharacterArgs();
            args.MatchId = matchId;
            args.UserProfileId = userId;
            args.SecretCharacterId = characterId;
            return args;
        }

        private void SetupSuccessfulTurnInit()
        {
            turnRepoMock.Setup(r => r.GetTurnState(MATCH_ID)).Returns((TurnStateSnapshot)null);
            matchRepoMock.Setup(r => r.GetActivePlayerIds(MATCH_ID, 2))
                .Returns(new List<long> { USER_ID, OPPONENT_ID });
            turnRepoMock.Setup(r => r.InitializeTurnOrder(It.IsAny<InitializeTurnOrderArgs>()))
                .Returns(InitializeTurnOrderResult.Success());
        }
    }
}