using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Coordinators.Match;
using GuessWhoServices.Infrastructure;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;

namespace GuessWhoTests.Services.Coordinators.Match
{
    [TestClass]
    public class MatchLifecycleLogicTests
    {
        private const long MATCH_ID = 500;
        private const long USER_ID = 100;
        private const string MATCH_CODE = "123456";

        private Mock<IGuessWhoUnitOfWorkFactory> factoryMock;
        private Mock<IGuessWhoUnitOfWork> uowMock;
        private Mock<IMatchRepository> matchRepoMock;
        private Mock<IMatchCallbackDispatcher> dispatcherMock;
        private MatchLifecycleLogic logic;

        [TestInitialize]
        public void TestInitialize()
        {
            factoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            uowMock = new Mock<IGuessWhoUnitOfWork>();
            matchRepoMock = new Mock<IMatchRepository>();
            dispatcherMock = new Mock<IMatchCallbackDispatcher>();

            factoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            uowMock.Setup(u => u.Matches).Returns(matchRepoMock.Object);

            logic = new MatchLifecycleLogic(factoryMock.Object, dispatcherMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new MatchLifecycleLogic(null, dispatcherMock.Object);
        }

        [TestMethod]
        public void TestCreateMatch_InvalidHostId_ShouldReturnInvalidSnapshot()
        {
            MatchSnapshot result = logic.CreateMatch(0, DateTime.UtcNow);

            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void TestCreateMatch_SuccessOnFirstAttempt_ShouldReturnValidSnapshot()
        {
            MatchSnapshot validSnapshot = CreateSnapshot(MATCH_ID, MatchVisibility.Private);

            matchRepoMock.Setup(r => r.CreateMatchClassic(It.IsAny<CreateMatchArgs>()))
                .Returns(validSnapshot);

            MatchSnapshot result = logic.CreateMatch(USER_ID, DateTime.UtcNow);

            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void TestCreateMatch_SuccessAfterRetries_ShouldReturnValidSnapshot()
        {
            MatchSnapshot validSnapshot = CreateSnapshot(MATCH_ID, MatchVisibility.Private);

            matchRepoMock.SetupSequence(r => r.CreateMatchClassic(It.IsAny<CreateMatchArgs>()))
                .Returns(MatchSnapshot.CreateInvalid())
                .Returns(MatchSnapshot.CreateInvalid())
                .Returns(validSnapshot);

            MatchSnapshot result = logic.CreateMatch(USER_ID, DateTime.UtcNow);

            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void TestCreateMatch_FailAfterAllRetries_ShouldReturnInvalidSnapshot()
        {
            matchRepoMock.Setup(r => r.CreateMatchClassic(It.IsAny<CreateMatchArgs>()))
                .Returns(MatchSnapshot.CreateInvalid());

            MatchSnapshot result = logic.CreateMatch(USER_ID, DateTime.UtcNow);

            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void TestStartMatch_InvalidArgs_ShouldReturnInvalidArgs()
        {
            StartMatchResult result = logic.StartMatch(0, USER_ID);

            Assert.AreEqual(StartMatchResultCode.InvalidArgs, result.Code);
        }

        [TestMethod]
        public void TestStartMatch_Success_ShouldBroadcastAndReturnSuccess()
        {
            matchRepoMock.Setup(r => r.StartMatch(MATCH_ID, USER_ID))
                .Returns(StartMatchResult.Success());

            StartMatchResult result = logic.StartMatch(MATCH_ID, USER_ID);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestStartMatch_RepoFailure_ShouldReturnErrorCode()
        {
            matchRepoMock.Setup(r => r.StartMatch(MATCH_ID, USER_ID))
                .Returns(StartMatchResult.Fail(StartMatchResultCode.MatchNotFound));

            StartMatchResult result = logic.StartMatch(MATCH_ID, USER_ID);

            Assert.AreEqual(StartMatchResultCode.MatchNotFound, result.Code);
        }

        [TestMethod]
        public void TestEndMatch_InvalidMatchId_ShouldReturnInvalidArgs()
        {
            EndMatchResult result = logic.EndMatch(0);

            Assert.AreEqual(EndMatchResultCode.InvalidArgs, (EndMatchResultCode)result.Code);
        }

        [TestMethod]
        public void TestEndMatch_Success_ShouldBroadcastAndReturnSuccess()
        {
            matchRepoMock.Setup(r => r.EndMatch(It.IsAny<EndMatchArgs>()))
                .Returns(EndMatchResult.Success(USER_ID));

            EndMatchResult result = logic.EndMatch(MATCH_ID);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestSetMatchPrivate_InvalidArgs_ShouldReturnInvalidArgs()
        {
            SetMatchPrivateResult result = logic.SetMatchPrivate(0, USER_ID);

            Assert.AreEqual(SetMatchPrivateResultCode.InvalidArgs, result.Code);
        }

        [TestMethod]
        public void TestSetMatchPrivate_Success_ShouldReturnSuccess()
        {
            matchRepoMock.Setup(r => r.SetMatchPrivate(MATCH_ID, USER_ID))
                .Returns(SetMatchPrivateResult.Success());

            SetMatchPrivateResult result = logic.SetMatchPrivate(MATCH_ID, USER_ID);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestSearchPublicMatch_ValidPublicMatch_ShouldReturnSnapshot()
        {
            MatchSnapshot publicSnapshot = CreateSnapshot(MATCH_ID, MatchVisibility.Public);

            matchRepoMock.Setup(r => r.GetOpenMatchByCode(MATCH_CODE))
                .Returns(publicSnapshot);

            MatchSnapshot result = logic.SearchPublicMatch(MATCH_CODE);

            Assert.AreEqual(MATCH_ID, result.MatchId);
        }

        [TestMethod]
        public void TestSearchPublicMatch_PrivateMatch_ShouldReturnInvalid()
        {
            MatchSnapshot privateSnapshot = CreateSnapshot(MATCH_ID, MatchVisibility.Private);

            matchRepoMock.Setup(r => r.GetOpenMatchByCode(MATCH_CODE))
                .Returns(privateSnapshot);

            MatchSnapshot result = logic.SearchPublicMatch(MATCH_CODE);

            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void TestSearchPublicMatch_NullCode_ShouldReturnInvalid()
        {
            MatchSnapshot result = logic.SearchPublicMatch(null);

            Assert.IsFalse(result.IsValid);
        }

        private MatchSnapshot CreateSnapshot(long id, MatchVisibility visibility)
        {
            return new MatchSnapshot(
                id,
                MATCH_CODE,
                (byte)MatchStatus.Lobby,
                (byte)visibility,
                (byte)MatchMode.Classic,
                DateTime.UtcNow
            );
        }
    }
}