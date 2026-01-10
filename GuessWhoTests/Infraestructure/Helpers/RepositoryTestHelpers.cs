using Moq;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace GuessWhoTests.Infrastructure.Helpers
{
    public static class RepositoryTestHelpers
    {
        public static Mock<DbSet<T>> CreateEmptyDbSet<T>() where T : class
        {
            return CreateDbSet(new List<T>());
        }

        public static Mock<DbSet<T>> CreateDbSet<T>(IEnumerable<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            mockSet.Setup(m => m.AsNoTracking()).Returns(mockSet.Object);
            mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>((entity) =>
            {
                var list = queryable.ToList();
                list.Add(entity);
                queryable = list.AsQueryable();
                mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
                mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
                mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
                mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            });

            return mockSet;
        }
    }
}
