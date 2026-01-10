using GuessWhoDataAccess.Data;
using GuessWhoDataAccess.Data.DataAccess.Characters;
using GuessWhoTests.Infrastructure;
using GuessWhoTests.Infrastructure.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuessWhoTests.Data.DataAccess.Characters
{
    [TestClass]
    public class CharacterDataTests : RepositoryTestBase
    {
        private const string CHARACTER_ID_1 = "CH001";
        private const string CHARACTER_ID_2 = "CH002";
        private const string INVALID_EMPTY_ID = "";
        private const string INVALID_WHITESPACE_ID = "   ";

        private CharacterData characterRepository;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            characterRepository = new CharacterData(MockContext.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullContext_ThrowsArgumentNullException()
        {
            new CharacterData(null);
        }

        [TestMethod]
        public void TestGetActiveCharacterIds_ValidActiveCharacters_ReturnsOnlyValidIds()
        {
            var characters = new List<CHARACTER>
            {
                new CHARACTER { CHARACTERID = CHARACTER_ID_1, ISACTIVE = true },
                new CHARACTER { CHARACTERID = CHARACTER_ID_2, ISACTIVE = true },
                new CHARACTER { CHARACTERID = "CH003", ISACTIVE = false },
                new CHARACTER { CHARACTERID = null, ISACTIVE = true },
                new CHARACTER { CHARACTERID = INVALID_EMPTY_ID, ISACTIVE = true },
                new CHARACTER { CHARACTERID = INVALID_WHITESPACE_ID, ISACTIVE = true }
            };

            MockContext.Setup(c => c.CHARACTER)
                .Returns(RepositoryTestHelpers.CreateDbSet(characters).Object);

            var result = characterRepository.GetActiveCharacterIds();

            Assert.IsTrue(
                result.Count == 2 &&
                result.Contains(CHARACTER_ID_1) &&
                result.Contains(CHARACTER_ID_2));
        }

        [TestMethod]
        public void TestGetActiveCharacterIds_NoActiveCharacters_ReturnsEmptyList()
        {
            var characters = new List<CHARACTER>
            {
                new CHARACTER { CHARACTERID = CHARACTER_ID_1, ISACTIVE = false }
            };

            MockContext.Setup(c => c.CHARACTER)
                .Returns(RepositoryTestHelpers.CreateDbSet(characters).Object);

            var result = characterRepository.GetActiveCharacterIds();

            Assert.IsTrue(result.Count == 0);
        }
    }
}