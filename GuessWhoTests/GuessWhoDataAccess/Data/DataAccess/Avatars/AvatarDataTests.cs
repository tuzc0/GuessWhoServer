using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using GuessWhoDataAccess.Data;
using GuessWhoDataAccess.Data.DataAccess.Avatars;
using GuessWhoTests.Infrastructure;
using GuessWhoTests.Infrastructure.Helpers;

namespace GuessWhoTests.Data.DataAccess.Avatars
{
    [TestClass]
    public class AvatarDataTests : RepositoryTestBase
    {
        private const string AVATAR_ID_LUCIO = "A0001";
        private const string AVATAR_ID_LUCIA = "A0002";
        private const string AVATAR_NAME_LUCIO = "Lucio";
        private const string AVATAR_NAME_LUCIA = "Lucia";
        private const string OTHER_NAME = "Inactivo";
        private const string EMPTY_STRING = "";

        private AvatarData avatarRepository;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            avatarRepository = new AvatarData(MockContext.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullContext_ThrowsArgumentNullException()
        {
            new AvatarData(null);
        }

        [TestMethod]
        public void TestGetActiveAvatars_ReturnsOnlyActiveAvatars()
        {
            var avatars = new List<AVATAR>
            {
                new AVATAR { AVATARID = AVATAR_ID_LUCIO, NAME = AVATAR_NAME_LUCIO, ISACTIVE = true, ISDEFAULT = true },
                new AVATAR { AVATARID = AVATAR_ID_LUCIA, NAME = AVATAR_NAME_LUCIA, ISACTIVE = true, ISDEFAULT = false },
                new AVATAR { AVATARID = "A0003", NAME = OTHER_NAME, ISACTIVE = false, ISDEFAULT = false }
            };

            MockContext.Setup(c => c.AVATAR)
                .Returns(RepositoryTestHelpers.CreateDbSet(avatars).Object);

            var result = avatarRepository.GetActiveAvatars();

            Assert.IsTrue(
                result.Count == 2 &&
                result.Any(a => a.AvatarId == AVATAR_ID_LUCIO) &&
                result.Any(a => a.AvatarId == AVATAR_ID_LUCIA));
        }

        [TestMethod]
        public void TestGetActiveAvatars_EmptyIfNoneActive()
        {
            var avatars = new List<AVATAR>
            {
                new AVATAR { AVATARID = AVATAR_ID_LUCIO, ISACTIVE = false }
            };

            MockContext.Setup(c => c.AVATAR)
                .Returns(RepositoryTestHelpers.CreateDbSet(avatars).Object);

            var result = avatarRepository.GetActiveAvatars();

            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public void TestGetDefaultAvatarId_ExistingDefaultActive_ReturnsId()
        {
            var avatars = new List<AVATAR>
            {
                new AVATAR { AVATARID = AVATAR_ID_LUCIO, NAME = AVATAR_NAME_LUCIO, ISDEFAULT = true, ISACTIVE = true },
                new AVATAR { AVATARID = AVATAR_ID_LUCIA, NAME = AVATAR_NAME_LUCIA, ISDEFAULT = false, ISACTIVE = true }
            };

            MockContext.Setup(c => c.AVATAR)
                .Returns(RepositoryTestHelpers.CreateDbSet(avatars).Object);

            var result = avatarRepository.GetDefaultAvatarId();

            Assert.IsTrue(result == AVATAR_ID_LUCIO);
        }

        [TestMethod]
        public void TestGetDefaultAvatarId_DefaultInactive_ReturnsEmptyString()
        {
            var avatars = new List<AVATAR>
            {
                new AVATAR { AVATARID = AVATAR_ID_LUCIO, ISDEFAULT = true, ISACTIVE = false }
            };

            MockContext.Setup(c => c.AVATAR)
                .Returns(RepositoryTestHelpers.CreateDbSet(avatars).Object);

            var result = avatarRepository.GetDefaultAvatarId();

            Assert.IsTrue(result == EMPTY_STRING);
        }

        [TestMethod]
        public void TestGetDefaultAvatarId_NoDefaultInContext_ReturnsEmptyString()
        {
            MockContext.Setup(c => c.AVATAR)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<AVATAR>().Object);

            var result = avatarRepository.GetDefaultAvatarId();

            Assert.IsTrue(result == EMPTY_STRING);
        }
    }
}
