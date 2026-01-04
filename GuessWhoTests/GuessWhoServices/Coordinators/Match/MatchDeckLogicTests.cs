using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using GuessWhoCore.Contracts.Requests;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Coordinators.Match;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;

namespace GuessWhoTests.Services.Coordinators.Match
{
    [TestClass]
    public class MatchDeckLogicTests
    {
        private const long MATCH_ID = 100;
        private const byte MODE_CLASSIC = 1;
        private const byte MODE_TOURNAMENT = 2;

        private Mock<IGuessWhoUnitOfWorkFactory> factoryMock;
        private Mock<IGuessWhoUnitOfWork> uowMock;
        private Mock<IMatchDeckRepository> deckRepoMock;
        private Mock<ICharacterRepository> charRepoMock;
        private Mock<IGuessWhoDbTransaction> transactionMock;
        private MatchDeckLogic logic;

        [TestInitialize]
        public void TestInitialize()
        {
            factoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            uowMock = new Mock<IGuessWhoUnitOfWork>();
            deckRepoMock = new Mock<IMatchDeckRepository>();
            charRepoMock = new Mock<ICharacterRepository>();
            transactionMock = new Mock<IGuessWhoDbTransaction>();

            factoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            uowMock.Setup(u => u.MatchDecks).Returns(deckRepoMock.Object);
            uowMock.Setup(u => u.Characters).Returns(charRepoMock.Object);
            uowMock.Setup(u => u.BeginTransaction()).Returns(transactionMock.Object);

            logic = new MatchDeckLogic(factoryMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new MatchDeckLogic(null);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_NullRequest_ShouldReturnInvalidArgs()
        {
            var result = logic.GetOrCreateMatchDeck(null);

            Assert.AreEqual(MatchDeckResultCode.InvalidArgs, result.Code);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_InvalidMatchId_ShouldReturnInvalidMatchId()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = 0 };

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.AreEqual(MatchDeckResultCode.InvalidMatchId, result.Code);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_ExistingDeckEarlyReturn_ShouldReturnSuccess()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID };
            var existing = MatchDeckResult.Success(MATCH_ID, new List<string> { "C1" });
            deckRepoMock.Setup(r => r.GetMatchDeck(MATCH_ID)).Returns(existing);

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_InsufficientCharactersClassic_ShouldReturnError()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            deckRepoMock.Setup(r => r.GetMatchDeck(MATCH_ID)).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound));
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(new List<string>(new string[23]));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.AreEqual(MatchDeckResultCode.InsufficientCharacters, result.Code);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_InsufficientCharactersTournament_ShouldReturnError()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_TOURNAMENT };
            deckRepoMock.Setup(r => r.GetMatchDeck(MATCH_ID)).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound));
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(new List<string>(new string[29]));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.AreEqual(MatchDeckResultCode.InsufficientCharacters, result.Code);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_CandidateCleaning_ShouldFilterNullsAndDuplicates()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            deckRepoMock.Setup(r => r.GetMatchDeck(MATCH_ID)).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound));
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(new List<string> { "C1", "c1", null, "  ", "C2" });

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.AreEqual(MatchDeckResultCode.InsufficientCharacters, result.Code);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_ConcurrencyRecheckSuccess_ShouldReturnExisting()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Repeat("C", 30).Select((s, i) => s + i).ToList());
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Success(MATCH_ID, new List<string> { "C1" }));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_CreationSucceeds_ShouldReturnDeck()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Range(1, 30).Select(i => i.ToString()).ToList());
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Success(MATCH_ID, new List<string> { "C1" }));
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Success(MATCH_ID, new List<string>()));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_CreationConflictAlreadyExists_ShouldReload()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Repeat("C", 30).Select((s, i) => s + i).ToList());
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckAlreadyExists));
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Success(MATCH_ID, new List<string> { "C1" }));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_CreationGenericFailure_ShouldReturnError()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Repeat("C", 30).Select((s, i) => s + i).ToList());
            deckRepoMock.Setup(r => r.GetMatchDeck(MATCH_ID)).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound));
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckGenerationFailed));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.AreEqual(MatchDeckResultCode.DeckGenerationFailed, result.Code);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_FinalVerificationFails_ShouldReturnGenerationFailed()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Repeat("C", 30).Select((s, i) => s + i).ToList());
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound));
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Success(MATCH_ID, new List<string>()));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.AreEqual(MatchDeckResultCode.DeckGenerationFailed, result.Code);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_TransactionRollbackOnFail_ShouldVerifyRollback()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Repeat("C", 30).Select((s, i) => s + i).ToList());
            deckRepoMock.Setup(r => r.GetMatchDeck(MATCH_ID)).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound));
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckGenerationFailed));

            logic.GetOrCreateMatchDeck(request);

            transactionMock.Verify(t => t.Rollback(), Times.Once());
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_TransactionCommitOnSuccess_ShouldVerifyCommit()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Range(1, 30).Select(i => i.ToString()).ToList());
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Success(MATCH_ID, new List<string> { "C1" }));
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Success(MATCH_ID, new List<string>()));

            logic.GetOrCreateMatchDeck(request);

            transactionMock.Verify(t => t.Commit(), Times.Once());
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_ShuffleIntegration_ShouldProcessWithoutErrors()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Range(1, 24).Select(i => "C" + i).ToList());
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Success(MATCH_ID, new List<string> { "C1" }));
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Success(MATCH_ID, new List<string>()));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_EarlyRecheckSuccess_ShouldCommitAndReturn()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Repeat("C", 30).Select((s, i) => s + i).ToList());
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Success(MATCH_ID, new List<string> { "C1" }));

            logic.GetOrCreateMatchDeck(request);

            transactionMock.Verify(t => t.Commit(), Times.Once());
        }

        [TestMethod]
        public void TestGetOrCreateMatchDeck_ReloadFailsAfterConflict_ShouldReturnOperationConflict()
        {
            var request = new GetOrCreateMatchDeckRequest { MatchId = MATCH_ID, ModeId = MODE_CLASSIC };
            charRepoMock.Setup(r => r.GetActiveCharacterIds()).Returns(Enumerable.Repeat("C", 30).Select((s, i) => s + i).ToList());
            deckRepoMock.SetupSequence(r => r.GetMatchDeck(MATCH_ID))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound))
                .Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound));
            deckRepoMock.Setup(r => r.CreateDeck(It.IsAny<SaveMatchDeckArgs>())).Returns(MatchDeckResult.Fail(MatchDeckResultCode.DeckAlreadyExists));

            var result = logic.GetOrCreateMatchDeck(request);

            Assert.AreEqual(MatchDeckResultCode.OperationConflict, result.Code);
        }
    }
}