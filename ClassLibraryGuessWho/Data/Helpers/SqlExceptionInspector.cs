using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace GuessWhoDataAccess.Data.Helpers
{
    public static class SqlExceptionInspector
    {
        private const int ERROR_UNIQUE_INDEX_VIOLATION = 2601;
        private const int ERROR_DUPLICATE_KEY_VIOLATION = 2627;

        public static bool IsUniqueConstraintViolation(Exception exception)
        {
            SqlException sqlException = ExtractSqlException(exception);
            
            if (sqlException == null)
            {
                return false;
            }

            return sqlException.Number == ERROR_UNIQUE_INDEX_VIOLATION ||
                   sqlException.Number == ERROR_DUPLICATE_KEY_VIOLATION;
        }

        private static SqlException ExtractSqlException(Exception rootException)
        {
            for (Exception current = rootException; current != null; current = current.InnerException)
            {
                if (current is SqlException directSqlException)
                {
                    return directSqlException;
                }

                if (current is DbUpdateException dbUpdateException &&
                    dbUpdateException.InnerException?.InnerException is SqlException nestedSqlException)
                {
                    return nestedSqlException;
                }
            }

            return null;
        }
    }
}
