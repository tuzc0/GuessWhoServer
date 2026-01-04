using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Request;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoServices.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace GuessWhoTests.Services
{
    [TestClass]
    public class FriendServiceTests
    {
        private const string ACCOUNT_ID_STR = "10";
        private const string FRIEND_REQ_ID_STR = "500";
        private const string INVALID_ID_STR = "abc";
        private const string DISPLAY_NAME = "Juan";
        private const string AVATAR_ID = "A0001";
        private const string EMPTY_STRING = "";
        private const long TO_USER_ID = 20;
        private const long FROM_ACCOUNT_ID = 10;
        private const long FRIEND_REQ_ID = 500;
        private const byte STATUS_PENDING = 1;

        private Mock<IFriendshipManager> friendshipMock;
        private FriendService service;

        [TestInitialize]
        public void TestInitialize()
        {
            friendshipMock = new Mock<IFriendshipManager>();
            service = new FriendService(friendshipMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullManager_ShouldThrowException()
        {
            new FriendService(null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSearchProfiles_NullRequest_ShouldThrowFaultException()
        {
            service.SearchProfiles(null);
        }

        [TestMethod]
        public void TestSearchProfiles_Success_ShouldReturnMappedProfileCount()
        {
            SearchProfileRequest request = new SearchProfileRequest { DisplayName = DISPLAY_NAME };
            List<UserProfileSearchRecord> mockData = new List<UserProfileSearchRecord>
            {
                new UserProfileSearchRecord(FROM_ACCOUNT_ID, DISPLAY_NAME, AVATAR_ID)
            };
            friendshipMock.Setup(m => m.SearchProfiles(DISPLAY_NAME)).Returns(mockData);

            SearchProfilesResponse response = service.SearchProfiles(request);

            Assert.AreEqual(1, response.Profiles.Count);
        }

        [TestMethod]
        public void TestSearchProfiles_Success_ShouldReturnMappedDisplayName()
        {
            SearchProfileRequest request = new SearchProfileRequest { DisplayName = DISPLAY_NAME };
            List<UserProfileSearchRecord> mockData = new List<UserProfileSearchRecord>
            {
                new UserProfileSearchRecord(FROM_ACCOUNT_ID, DISPLAY_NAME, AVATAR_ID)
            };
            friendshipMock.Setup(m => m.SearchProfiles(DISPLAY_NAME)).Returns(mockData);

            SearchProfilesResponse response = service.SearchProfiles(request);

            Assert.AreEqual(DISPLAY_NAME, response.Profiles[0].DisplayName);
        }

        [TestMethod]
        public void TestSendFriendRequest_ManagerReturnsNull_ShouldReturnSuccessFalse()
        {
            SendFriendRequestRequest request = new SendFriendRequestRequest { FromAccountId = FROM_ACCOUNT_ID, ToUserId = TO_USER_ID };
            friendshipMock.Setup(m => m.SendFriendRequest(FROM_ACCOUNT_ID, TO_USER_ID, It.IsAny<DateTime>()))
                .Returns((SendFriendRequestResult)null);

            SendFriendRequestResponse response = service.SendFriendRequest(request);

            Assert.IsFalse(response.Success);
        }

        [TestMethod]
        public void TestSendFriendRequest_ManagerReturnsNull_ShouldReturnEmptyFriendRequestId()
        {
            SendFriendRequestRequest request = new SendFriendRequestRequest { FromAccountId = FROM_ACCOUNT_ID, ToUserId = TO_USER_ID };
            friendshipMock.Setup(m => m.SendFriendRequest(FROM_ACCOUNT_ID, TO_USER_ID, It.IsAny<DateTime>()))
                .Returns((SendFriendRequestResult)null);

            SendFriendRequestResponse response = service.SendFriendRequest(request);

            Assert.AreEqual(EMPTY_STRING, response.FriendRequestId);
        }

        [TestMethod]
        public void TestSendFriendRequest_Success_ShouldReturnAutoAcceptedTrue()
        {
            SendFriendRequestRequest request = new SendFriendRequestRequest { FromAccountId = FROM_ACCOUNT_ID, ToUserId = TO_USER_ID };
            SendFriendRequestResult expectedResult = SendFriendRequestResult.OkAutoAccepted();
            friendshipMock.Setup(m => m.SendFriendRequest(FROM_ACCOUNT_ID, TO_USER_ID, It.IsAny<DateTime>()))
                .Returns(expectedResult);

            SendFriendRequestResponse response = service.SendFriendRequest(request);

            Assert.IsTrue(response.AutoAccepted);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestAcceptFriendRequest_InvalidAccountId_ShouldThrowFaultException()
        {
            FriendRequestOperationRequest request = new FriendRequestOperationRequest { AccountId = INVALID_ID_STR, FriendRequestId = FRIEND_REQ_ID_STR };
            service.AcceptFriendRequest(request);
        }

        [TestMethod]
        public void TestAcceptFriendRequest_Success_ShouldReturnSuccessTrue()
        {
            FriendRequestOperationRequest request = new FriendRequestOperationRequest { AccountId = ACCOUNT_ID_STR, FriendRequestId = FRIEND_REQ_ID_STR };
            friendshipMock.Setup(m => m.AcceptFriendRequest(It.IsAny<FriendRequestActionArgs>())).Returns(true);

            BasicResponse response = service.AcceptFriendRequest(request);

            Assert.IsTrue(response.Success);
        }

        [TestMethod]
        public void TestGetFriends_EmptyList_ShouldReturnNonNullList()
        {
            GetFriendsRequest request = new GetFriendsRequest { AccountId = ACCOUNT_ID_STR };
            friendshipMock.Setup(m => m.GetFriends(ACCOUNT_ID_STR)).Returns(new List<UserProfileSearchRecord>());

            GetFriendsResponse response = service.GetFriends(request);

            Assert.IsNotNull(response.Friends);
        }

        [TestMethod]
        public void TestGetPendingRequests_Success_ShouldReturnCorrectRequestCount()
        {
            GetPendingFriendRequestsRequest request = new GetPendingFriendRequestsRequest { AccountId = ACCOUNT_ID_STR };
            List<FriendRequestRecord> mockRecords = new List<FriendRequestRecord>
            {
                new FriendRequestRecord(FRIEND_REQ_ID, 1, 2, DISPLAY_NAME, STATUS_PENDING, DateTime.UtcNow)
            };
            friendshipMock.Setup(m => m.GetPendingRequests(ACCOUNT_ID_STR)).Returns(mockRecords);

            GetPendingRequestsResponse response = service.GetPendingRequests(request);

            Assert.AreEqual(1, response.Requests.Count);
        }

        [TestMethod]
        public void TestGetPendingRequests_Success_ShouldMapFriendRequestId()
        {
            GetPendingFriendRequestsRequest request = new GetPendingFriendRequestsRequest { AccountId = ACCOUNT_ID_STR };
            List<FriendRequestRecord> mockRecords = new List<FriendRequestRecord>
            {
                new FriendRequestRecord(FRIEND_REQ_ID, 1, 2, DISPLAY_NAME, STATUS_PENDING, DateTime.UtcNow)
            };
            friendshipMock.Setup(m => m.GetPendingRequests(ACCOUNT_ID_STR)).Returns(mockRecords);

            GetPendingRequestsResponse response = service.GetPendingRequests(request);

            Assert.AreEqual(FRIEND_REQ_ID, response.Requests[0].FriendRequestId);
        }
    }
}