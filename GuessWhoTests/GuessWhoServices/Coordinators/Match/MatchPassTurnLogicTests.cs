using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Turns;
using GuessWhoServices.Coordinators.Match;

namespace GuessWhoTests.Coordinators.Match
{
    [TestClass]
    public class MatchPassTurnLogicTests
    {
        private const long VALID_MATCH_ID = 100;
        private const long VALID_USER_ID = 1;
        private const long OPPONENT_ID = 2;
        private const long INVALID_ID = 0;
        private const int CONSUMED_NORMAL = 30;
        private const int CONSUMED_TIMEOUT = 100;
        private const int LIMIT_SECONDS = 60;
        private const string TEST_CODE = "MATCH_001";
        private const byte STATUS_ACTIVE = (byte)MatchStatus.Active;
        private const byte STATUS_FINISHED = (byte)MatchStatus.Finished;

        private Mock<IGuessWhoUnitOfWorkFactory> factoryMock;
        private Mock<IGuessWhoUnitOfWork> unitOfWorkMock;
        private Mock<IGuessWhoDbTransaction> transactionMock;
        private Mock<IMatchChessClockRepository> chessClockRepoMock;
        private Mock<IMatchRepository> matchRepoMock;
        private Mock<IMatchTurnAdvanceRepository> turnAdvanceRepoMock;
        private Mock<IMatchTurnRepository> turnRepoMock;
        private MatchPassTurnLogic logic;

        [TestInitialize]
        public void TestInitialize()
        {
            factoryMock = new();
            unitOfWorkMock = new();
            transactionMock = new();
            chessClockRepoMock = new();
            matchRepoMock = new();
            turnAdvanceRepoMock = new();
            turnRepoMock = new();

            unitOfWorkMock.Setup(u => u.BeginTransaction()).Returns(transactionMock.Object);
            unitOfWorkMock.Setup(u => u.MatchChessClocks).Returns(chessClockRepoMock.Object);
            unitOfWorkMock.Setup(u => u.Matches).Returns(matchRepoMock.Object);
            unitOfWorkMock.Setup(u => u.MatchTurnAdvances).Returns(turnAdvanceRepoMock.Object);
            unitOfWorkMock.Setup(u => u.MatchesTurns).Returns(turnRepoMock.Object);
            factoryMock.Setup(f => f.Create()).Returns(unitOfWorkMock.Object);

            logic = new(factoryMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new MatchPassTurnLogic(null);
        }

        [TestMethod]
        public void TestPassTurn_NullArgs_ShouldReturnInvalidArgs()
        {
            PassTurnOutcome result = logic.PassTurn(null);
            Assert.AreEqual(PassTurnOutcomeCode.InvalidArgs, result.Code);
        }

        [TestMethod]
        public void TestPassTurn_InvalidMatchId_ShouldReturnInvalidArgs()
        {
            PassTurnArgs args = new() { MatchId = INVALID_ID, UserId = VALID_USER_ID };
            PassTurnOutcome result = logic.PassTurn(args);
            Assert.AreEqual(PassTurnOutcomeCode.InvalidArgs, result.Code);
        }

        [TestMethod]
        public void TestPassTurn_ClockApplyFails_ShouldReturnOperationConflict()
        {
            PassTurnArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            ApplyChessClockResult clockResult = ApplyChessClockResult.Fail(ApplyChessClockResultCode.OperationConflict);
            chessClockRepoMock.Setup(r => r.ApplyOnPassTurn(It.IsAny<ApplyChessClockArgs>())).Returns(clockResult);

            PassTurnOutcome result = logic.PassTurn(args);
            Assert.AreEqual(PassTurnOutcomeCode.OperationConflict, result.Code);
        }

        [TestMethod]
        public void TestPassTurn_TimeOutHandleDisconnectFails_ShouldReturnOperationConflict()
        {
            PassTurnArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            ApplyChessClockResult clockResult = ApplyChessClockResult.Success(CONSUMED_TIMEOUT, LIMIT_SECONDS, OPPONENT_ID);
            DisconnectMatchResult disconnectResult = DisconnectMatchResult.Fail(DisconnectMatchResultCode.OperationConflict);

            chessClockRepoMock.Setup(r => r.ApplyOnPassTurn(It.IsAny<ApplyChessClockArgs>())).Returns(clockResult);
            matchRepoMock.Setup(r => r.HandleDisconnect(VALID_USER_ID, It.IsAny<DateTime>())).Returns(disconnectResult);

            PassTurnOutcome result = logic.PassTurn(args);
            Assert.AreEqual(PassTurnOutcomeCode.OperationConflict, result.Code);
        }

        [TestMethod]
        public void TestPassTurn_TimeOutSuccess_ShouldReturnTimeOutCode()
        {
            PassTurnArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            ApplyChessClockResult clockResult = ApplyChessClockResult.Success(CONSUMED_TIMEOUT, LIMIT_SECONDS, OPPONENT_ID);
            DisconnectMatchResult disconnectResult = new(DisconnectMatchResultCode.SuccessEndedWithWinner, VALID_MATCH_ID, OPPONENT_ID);

            chessClockRepoMock.Setup(r => r.ApplyOnPassTurn(It.IsAny<ApplyChessClockArgs>())).Returns(clockResult);
            matchRepoMock.Setup(r => r.HandleDisconnect(VALID_USER_ID, It.IsAny<DateTime>())).Returns(disconnectResult);

            PassTurnOutcome result = logic.PassTurn(args);
            Assert.AreEqual(PassTurnOutcomeCode.TimeOut, result.Code);
        }

        [TestMethod]
        public void TestPassTurn_AdvanceTurnFails_ShouldReturnOperationConflict()
        {
            PassTurnArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            ApplyChessClockResult clockResult = ApplyChessClockResult.Success(CONSUMED_NORMAL, LIMIT_SECONDS, OPPONENT_ID);
            AdvanceTurnResult advanceResult = AdvanceTurnResult.Fail(AdvanceTurnResultCode.OperationConflict);

            chessClockRepoMock.Setup(r => r.ApplyOnPassTurn(It.IsAny<ApplyChessClockArgs>())).Returns(clockResult);
            turnAdvanceRepoMock.Setup(r => r.AdvanceTurn(It.IsAny<AdvanceTurnArgs>())).Returns(advanceResult);

            PassTurnOutcome result = logic.PassTurn(args);
            Assert.AreEqual(PassTurnOutcomeCode.OperationConflict, result.Code);
        }

        [TestMethod]
        public void TestPassTurn_Success_ShouldReturnSuccessCode()
        {
            PassTurnArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            ApplyChessClockResult clockResult = ApplyChessClockResult.Success(CONSUMED_NORMAL, LIMIT_SECONDS, OPPONENT_ID);
            TurnStateSnapshot snapshot = new(VALID_MATCH_ID, 1, 1, OPPONENT_ID, DateTime.UtcNow);
            AdvanceTurnResult advanceResult = AdvanceTurnResult.Success(snapshot);

            chessClockRepoMock.Setup(r => r.ApplyOnPassTurn(It.IsAny<ApplyChessClockArgs>())).Returns(clockResult);
            turnAdvanceRepoMock.Setup(r => r.AdvanceTurn(It.IsAny<AdvanceTurnArgs>())).Returns(advanceResult);

            PassTurnOutcome result = logic.PassTurn(args);
            Assert.AreEqual(PassTurnOutcomeCode.Success, result.Code);
        }

        [TestMethod]
        public void TestClaimTimeout_MatchNotFound_ShouldReturnMatchNotFound()
        {
            ClaimTimeoutArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            matchRepoMock.Setup(r => r.GetMatchById(VALID_MATCH_ID)).Returns(MatchSnapshot.CreateInvalid());

            ClaimTimeoutResult result = logic.ClaimTimeout(args);
            Assert.AreEqual(ClaimTimeoutResultCode.MatchNotFound, result.Code);
        }

        [TestMethod]
        public void TestClaimTimeout_MatchNotActive_ShouldReturnMatchNotInProgress()
        {
            ClaimTimeoutArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            MatchSnapshot snapshot = new(VALID_MATCH_ID, TEST_CODE, STATUS_FINISHED, 1, 1, DateTime.UtcNow);
            matchRepoMock.Setup(r => r.GetMatchById(VALID_MATCH_ID)).Returns(snapshot);

            ClaimTimeoutResult result = logic.ClaimTimeout(args);
            Assert.AreEqual(ClaimTimeoutResultCode.MatchNotInProgress, result.Code);
        }

        [TestMethod]
        public void TestClaimTimeout_ClaimOnOwnTurn_ShouldReturnCannotClaimOnYourTurn()
        {
            ClaimTimeoutArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };
            MatchSnapshot mSnapshot = new(VALID_MATCH_ID, TEST_CODE, STATUS_ACTIVE, 1, 1, DateTime.UtcNow);
            TurnStateSnapshot tSnapshot = new(VALID_MATCH_ID, 1, 0, VALID_USER_ID, DateTime.UtcNow);

            matchRepoMock.Setup(r => r.GetMatchById(VALID_MATCH_ID)).Returns(mSnapshot);
            turnRepoMock.Setup(r => r.GetTurnState(VALID_MATCH_ID)).Returns(tSnapshot);

            ClaimTimeoutResult result = logic.ClaimTimeout(args);
            Assert.AreEqual(ClaimTimeoutResultCode.CannotClaimOnYourTurn, result.Code);
        }

        [TestMethod]
        public void TestClaimTimeout_NotTimedOut_ShouldReturnNotTimedOutCode()
        {
            ClaimTimeoutArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID, NowUtc = DateTime.UtcNow };
            MatchSnapshot mSnapshot = new(VALID_MATCH_ID, TEST_CODE, STATUS_ACTIVE, 1, 1, DateTime.UtcNow);
            TurnStateSnapshot tSnapshot = new(VALID_MATCH_ID, 1, 1, OPPONENT_ID, DateTime.UtcNow.AddSeconds(-5));
            MatchClockSnapshot cSnapshot = new(VALID_MATCH_ID, OPPONENT_ID, 10, LIMIT_SECONDS);

            matchRepoMock.Setup(r => r.GetMatchById(VALID_MATCH_ID)).Returns(mSnapshot);
            turnRepoMock.Setup(r => r.GetTurnState(VALID_MATCH_ID)).Returns(tSnapshot);
            chessClockRepoMock.Setup(r => r.GetPlayerClockState(VALID_MATCH_ID, OPPONENT_ID)).Returns(cSnapshot);

            ClaimTimeoutResult result = logic.ClaimTimeout(args);
            Assert.AreEqual(ClaimTimeoutResultCode.NotTimedOut, result.Code);
        }

        [TestMethod]
        public void TestClaimTimeout_Success_ShouldReturnSuccessCode()
        {
            ClaimTimeoutArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID, NowUtc = DateTime.UtcNow };
            MatchSnapshot mSnapshot = new(VALID_MATCH_ID, TEST_CODE, STATUS_ACTIVE, 1, 1, DateTime.UtcNow);
            TurnStateSnapshot tSnapshot = new(VALID_MATCH_ID, 1, 1, OPPONENT_ID, DateTime.UtcNow.AddMinutes(-10));
            MatchClockSnapshot cSnapshot = new(VALID_MATCH_ID, OPPONENT_ID, 50, LIMIT_SECONDS);

            matchRepoMock.Setup(r => r.GetMatchById(VALID_MATCH_ID)).Returns(mSnapshot);
            turnRepoMock.Setup(r => r.GetTurnState(VALID_MATCH_ID)).Returns(tSnapshot);
            chessClockRepoMock.Setup(r => r.GetPlayerClockState(VALID_MATCH_ID, OPPONENT_ID)).Returns(cSnapshot);
            matchRepoMock.Setup(r => r.EndMatch(It.IsAny<EndMatchArgs>())).Returns(EndMatchResult.Success(VALID_USER_ID));

            ClaimTimeoutResult result = logic.ClaimTimeout(args);
            Assert.AreEqual(ClaimTimeoutResultCode.Success, result.Code);
        }

        [TestMethod]
        public void TestClaimTimeout_EndMatchFails_ShouldReturnOperationConflict()
        {
            ClaimTimeoutArgs args = new() { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID, NowUtc = DateTime.UtcNow };
            MatchSnapshot mSnapshot = new(VALID_MATCH_ID, TEST_CODE, STATUS_ACTIVE, 1, 1, DateTime.UtcNow);
            TurnStateSnapshot tSnapshot = new(VALID_MATCH_ID, 1, 1, OPPONENT_ID, DateTime.UtcNow.AddMinutes(-10));
            MatchClockSnapshot cSnapshot = new(VALID_MATCH_ID, OPPONENT_ID, 50, LIMIT_SECONDS);

            matchRepoMock.Setup(r => r.GetMatchById(VALID_MATCH_ID)).Returns(mSnapshot);
            turnRepoMock.Setup(r => r.GetTurnState(VALID_MATCH_ID)).Returns(tSnapshot);
            chessClockRepoMock.Setup(r => r.GetPlayerClockState(VALID_MATCH_ID, OPPONENT_ID)).Returns(cSnapshot);
            matchRepoMock.Setup(r => r.EndMatch(It.IsAny<EndMatchArgs>())).Returns(EndMatchResult.Fail(EndMatchResultCode.ConcurrentUpdate));

            ClaimTimeoutResult result = logic.ClaimTimeout(args);
            Assert.AreEqual(ClaimTimeoutResultCode.OperationConflict, result.Code);
        }
    }
}