using GuessWhoDataAccess.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data.Entity;
using System.Linq;

namespace GuessWhoTests.Infrastructure
{
    [TestClass]
    public abstract class RepositoryTestBase
    {
        protected Mock<GuessWhoDBEntities> MockContext;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            MockContext = new Mock<GuessWhoDBEntities>();
        }

        protected Mock<DbSet<T>> SetupMockSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            return mockSet;
        }
    }
}