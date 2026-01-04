using GuessWhoServices.Coordinators;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Coordinators.Match;
using GuessWhoServices.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GuessWhoTests.Coordinators
{
    [TestClass]
    public class LobbySubscriptionOperationsTests
    {
        private const long VALID_MATCH_ID = 100;
        private const long VALID_USER_ID = 1;
        private const long INVALID_ID = 0;

        private Mock<ILobbySubscriptionStore> storeMock;
        private LobbySubscriptionOperations operations;

        [TestInitialize]
        public void TestInitialize()
        {
            storeMock = new Mock<ILobbySubscriptionStore>();
            operations = new LobbySubscriptionOperations(storeMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullStore_ShouldThrowException()
        {
            new LobbySubscriptionOperations(null);
        }

        [TestMethod]
        public void TestSubscribe_NullArgs_ShouldReturnFalse()
        {
            bool result = operations.Subscribe(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSubscribe_InvalidMatchId_ShouldReturnFalse()
        {
            LobbySubscriptionArgs args = new LobbySubscriptionArgs { MatchId = INVALID_ID, UserId = VALID_USER_ID };

            bool result = operations.Subscribe(args);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSubscribe_InvalidUserId_ShouldReturnFalse()
        {
            LobbySubscriptionArgs args = new LobbySubscriptionArgs { MatchId = VALID_MATCH_ID, UserId = INVALID_ID };

            bool result = operations.Subscribe(args);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSubscribe_NullContext_ShouldReturnFalse()
        {
            LobbySubscriptionArgs args = new LobbySubscriptionArgs { MatchId = VALID_MATCH_ID, UserId = VALID_USER_ID };

            bool result = operations.Subscribe(args);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUnsubscribe_NullArgs_ShouldReturnFalse()
        {
            bool result = operations.Unsubscribe(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestUnsubscribe_InvalidMatchId_ShouldReturnFalse()
        {
            LobbySubscriptionArgs args = new LobbySubscriptionArgs { MatchId = INVALID_ID, UserId = VALID_USER_ID };

            bool result = operations.Unsubscribe(args);

            Assert.IsFalse(result);
        }

    }
}