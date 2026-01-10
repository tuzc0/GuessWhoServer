using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using GuessWhoCore.Contracts.Faults;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using GuessWhoServerDomain.Domain.Results.Friends;
using GuessWhoServices.Coordinators;
using GuessWhoServerDomain.Domain.Enums.Friends;

namespace GuessWhoTests.Services.Coordinators
{
    [TestClass]
    public class FriendshipManagerTests
    {
        private const string ACCOUNT_ID_STR = "10";
        private const long ACCOUNT_ID = 10;
        private const long USER_ID = 100;
        private const long TARGET_USER_ID = 200;
        private const long FRIEND_REQUEST_ID = 500;
        private const string DISPLAY_NAME = "PlayerOne";
        private const string AVATAR_ID = "A001";
        private const long INVALID_ID = 0;

        private Mock<IGuessWhoUnitOfWorkFactory> unitOfWorkFactoryMock;
        private Mock<IGuessWhoUnitOfWork> unitOfWorkMock;
        private Mock<IFriendshipRepository> friendshipRepoMock;
        private Mock<IGuessWhoDbTransaction> transactionMock;
        private FriendshipManager manager;

        [TestInitialize]
        public void TestInitialize()
        {
            unitOfWorkFactoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            unitOfWorkMock = new Mock<IGuessWhoUnitOfWork>();
            friendshipRepoMock = new Mock<IFriendshipRepository>();
            transactionMock = new Mock<IGuessWhoDbTransaction>();

            unitOfWorkFactoryMock.Setup(f => f.Create()).Returns(unitOfWorkMock.Object);
            unitOfWorkMock.Setup(u => u.Friendships).Returns(friendshipRepoMock.Object);
            unitOfWorkMock.Setup(u => u.BeginTransaction()).Returns(transactionMock.Object);

            manager = new FriendshipManager(unitOfWorkFactoryMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new FriendshipManager(null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestGetFriends_InvalidAccountIdFormat_ShouldThrowFault()
        {
            manager.GetFriends("ABC");
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestGetFriends_AccountNotFound_ShouldThrowFault()
        {
            friendshipRepoMock.Setup(r => r.TryResolveUserIdFromAccountId(ACCOUNT_ID)).Returns(INVALID_ID);

            manager.GetFriends(ACCOUNT_ID_STR);
        }

        [TestMethod]
        public void TestGetFriends_ValidData_ShouldReturnList()
        {
            friendshipRepoMock.Setup(r => r.TryResolveUserIdFromAccountId(ACCOUNT_ID)).Returns(USER_ID);
            friendshipRepoMock.Setup(r => r.GetFriends(USER_ID)).Returns(new List<UserProfileSearchRecord>
            {
                new UserProfileSearchRecord(USER_ID, DISPLAY_NAME, AVATAR_ID)
            });

            var result = manager.GetFriends(ACCOUNT_ID_STR);

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void TestGetPendingRequests_ValidData_ShouldReturnList()
        {
            friendshipRepoMock.Setup(r => r.TryResolveUserIdFromAccountId(ACCOUNT_ID)).Returns(USER_ID);
            friendshipRepoMock.Setup(r => r.GetPendingRequests(USER_ID)).Returns(new List<FriendRequestRecord>());

            var result = manager.GetPendingRequests(ACCOUNT_ID_STR);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSearchProfiles_NullDisplayName_ShouldThrowFault()
        {
            manager.SearchProfiles(null);
        }

        [TestMethod]
        public void TestSearchProfiles_ValidData_ShouldReturnResults()
        {
            friendshipRepoMock.Setup(r => r.SearchProfilesByDisplayName(DISPLAY_NAME)).Returns(new List<UserProfileSearchRecord>());

            var result = manager.SearchProfiles(DISPLAY_NAME);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSendFriendRequest_ToSelf_ShouldThrowFault()
        {
            friendshipRepoMock.Setup(r => r.TryResolveUserIdFromAccountId(ACCOUNT_ID)).Returns(USER_ID);

            manager.SendFriendRequest(ACCOUNT_ID, USER_ID, DateTime.UtcNow);
        }

        [TestMethod]
        public void TestSendFriendRequest_AlreadyFriends_ShouldReturnResult()
        {
            friendshipRepoMock.Setup(r => r.TryResolveUserIdFromAccountId(ACCOUNT_ID)).Returns(USER_ID);
            friendshipRepoMock.Setup(r => r.IsUserProfileActive(TARGET_USER_ID)).Returns(true);
            friendshipRepoMock.Setup(r => r.AreAlreadyFriends(USER_ID, TARGET_USER_ID)).Returns(true);

            var result = manager.SendFriendRequest(ACCOUNT_ID, TARGET_USER_ID, DateTime.UtcNow);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestSendFriendRequest_InversePendingExists_ShouldReturnResultId()
        {
            friendshipRepoMock.Setup(r => r.TryResolveUserIdFromAccountId(ACCOUNT_ID)).Returns(USER_ID);
            friendshipRepoMock.Setup(r => r.IsUserProfileActive(TARGET_USER_ID)).Returns(true);
            friendshipRepoMock.Setup(r => r.TryAcceptInversePending(USER_ID, TARGET_USER_ID, It.IsAny<DateTime>()))
                .Returns(FriendRequestDataResult.AutoAccepted(FRIEND_REQUEST_ID));

            var result = manager.SendFriendRequest(ACCOUNT_ID, TARGET_USER_ID, DateTime.UtcNow);

            Assert.AreEqual(FRIEND_REQUEST_ID.ToString(), result.FriendRequestId.ToString());
        }

        [TestMethod]
        public void TestAcceptFriendRequest_ValidData_ShouldReturnTrue()
        {
            var args = new FriendRequestActionArgs(ACCOUNT_ID, FRIEND_REQUEST_ID, DateTime.UtcNow);
            friendshipRepoMock.Setup(r => r.AcceptFriendRequest(args)).Returns(FriendRequestDataResult.Accepted(FRIEND_REQUEST_ID));

            var result = manager.AcceptFriendRequest(args);

            Assert.IsTrue(result);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestAcceptFriendRequest_NotFound_ShouldThrowFault()
        {
            var args = new FriendRequestActionArgs(ACCOUNT_ID, FRIEND_REQUEST_ID, DateTime.UtcNow);
            friendshipRepoMock.Setup(r => r.AcceptFriendRequest(args)).Returns(FriendRequestDataResult.NotFound());

            manager.AcceptFriendRequest(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRejectFriendRequest_NotAuthorized_ShouldThrowFault()
        {
            var args = new FriendRequestActionArgs(ACCOUNT_ID, FRIEND_REQUEST_ID, DateTime.UtcNow);
            friendshipRepoMock.Setup(r => r.RejectFriendRequest(args)).Returns(FriendRequestDataResult.NotAuthorized());

            manager.RejectFriendRequest(args);
        }

        [TestMethod]
        public void TestCancelFriendRequest_ValidData_ShouldReturnTrue()
        {
            var args = new FriendRequestActionArgs(ACCOUNT_ID, FRIEND_REQUEST_ID, DateTime.UtcNow);
            friendshipRepoMock.Setup(r => r.CancelFriendRequest(args)).Returns(FriendRequestDataResult.Canceled(FRIEND_REQUEST_ID));

            var result = manager.CancelFriendRequest(args);

            Assert.IsTrue(result);
        }
    }
}