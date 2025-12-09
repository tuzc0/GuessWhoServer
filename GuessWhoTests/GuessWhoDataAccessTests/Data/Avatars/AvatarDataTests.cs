using ClassLibraryGuessWho.Data.DataAccess.Avatars;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GuessWhoTests.Integration.Data.DataAccess.Avatars
{
    [TestClass]
    public class AvatarDataTests
    {
        private AvatarData avatarData;

        private const string SEEDED_DEFAULT_ID = "A0001";
        private const string SEEDED_DEFAULT_NAME = "Lucio";
        private const int EXPECTED_ACTIVE_COUNT = 2;

        private const string MSG_COUNT_MATCH = "The returned list must contain the correct number of active avatars.";
        private const string MSG_ALL_ACTIVE = "All avatars in the list must be marked as active.";
        private const string MSG_DEFAULT_FOUND = "The default avatar must be present in the active list.";
        private const string MSG_DEFAULT_ID_MATCH = "The AvatarId of the default avatar must be mapped correctly.";
        private const string MSG_DEFAULT_NAME_MATCH = "The Name of the default avatar must be mapped correctly.";
        private const string MSG_DEFAULT_ISDEFAULT_TRUE = "The isDefault flag must be true for the default avatar.";
        private const string MSG_GET_DEFAULT_ID_MATCH = "Must return the correct ID of the avatar marked as default and active.";


        [TestInitialize]
        public void Setup()
        {
            DatabaseResetter.ResetDatabase();
            avatarData = new AvatarData();
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            DatabaseResetter.ResetDatabase();
        }

        [TestMethod]
        public void GetActiveAvatars_ShouldReturnCorrectCount()
        {
            List<AvatarDto> result = avatarData.GetActiveAvatars();

            Assert.AreEqual(EXPECTED_ACTIVE_COUNT, result.Count, MSG_COUNT_MATCH);
        }

        [TestMethod]
        public void GetActiveAvatars_AllShouldBeActive()
        {
            List<AvatarDto> result = avatarData.GetActiveAvatars();

            Assert.IsTrue(result.All(a => a.isActive), MSG_ALL_ACTIVE);
        }

        [TestMethod]
        public void GetActiveAvatars_ShouldContainDefaultAvatar()
        {
            List<AvatarDto> result = avatarData.GetActiveAvatars();
            AvatarDto defaultAvatar = result.FirstOrDefault(a => a.AvatarId == SEEDED_DEFAULT_ID);

            Assert.IsNotNull(defaultAvatar, MSG_DEFAULT_FOUND);
        }

        [TestMethod]
        public void GetActiveAvatars_DefaultAvatarShouldHaveCorrectId()
        {
            List<AvatarDto> result = avatarData.GetActiveAvatars();
            AvatarDto defaultAvatar = result.FirstOrDefault(a => a.isDefault);

            Assert.AreEqual(SEEDED_DEFAULT_ID, defaultAvatar.AvatarId, MSG_DEFAULT_ID_MATCH);
        }

        [TestMethod]
        public void GetActiveAvatars_DefaultAvatarShouldHaveCorrectName()
        {
            List<AvatarDto> result = avatarData.GetActiveAvatars();
            AvatarDto defaultAvatar = result.FirstOrDefault(a => a.isDefault);

            Assert.AreEqual(SEEDED_DEFAULT_NAME, defaultAvatar.Name, MSG_DEFAULT_NAME_MATCH);
        }

        [TestMethod]
        public void GetActiveAvatars_DefaultAvatarShouldBeMarkedAsDefault()
        {
            List<AvatarDto> result = avatarData.GetActiveAvatars();
            AvatarDto defaultAvatar = result.FirstOrDefault(a => a.AvatarId == SEEDED_DEFAULT_ID);

            Assert.IsTrue(defaultAvatar.isDefault, MSG_DEFAULT_ISDEFAULT_TRUE);
        }

        [TestMethod]
        public void GetDefaultAvatarId_ShouldReturnCorrectId()
        {
            string defaultId = avatarData.GetDefaultAvatarId();

            Assert.AreEqual(SEEDED_DEFAULT_ID, defaultId, MSG_GET_DEFAULT_ID_MATCH);
        }
    }
}