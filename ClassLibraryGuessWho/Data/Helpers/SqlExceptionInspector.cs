using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient; 

namespace ClassLibraryGuessWho.Data.Helpers
{
    public static class SqlExceptionInspector
    {

        private const int ERROR_FOREIGN_KEY_VIOLATION = 547;
        private const int ERROR_DEADLOCK = 1205;
        private const int ERROR_DATABASE_NOT_FOUND = 4060;
        private const int ERROR_TIMEOUT_EXPIRED = -2;
        private const int ERROR_CONNECTION_FAILURE = -1;
        private const int ERROR_SERVER_NOT_FOUND = 53;

        public static bool IsForeignKeyViolation(Exception exception)
            => TryExtractSqlException(exception, out var sqlException)
               && sqlException.Number == ERROR_FOREIGN_KEY_VIOLATION;

        public static bool IsDeadlockDetected(Exception exception)
            => TryExtractSqlException(exception, out var sqlException)
               && sqlException.Number == ERROR_DEADLOCK;

        public static bool IsDatabaseNotFound(Exception exception)
            => TryExtractSqlException(exception, out var sqlException)
               && sqlException.Number == ERROR_DATABASE_NOT_FOUND;

        public static bool IsCommandTimeout(Exception exception)
            => TryExtractSqlException(exception, out var sqlException)
               && sqlException.Number == ERROR_TIMEOUT_EXPIRED;

        public static bool IsConnectionFailure(Exception exception)
            => TryExtractSqlException(exception, out var sqlException)
               && (sqlException.Number == ERROR_CONNECTION_FAILURE || sqlException.Number == ERROR_SERVER_NOT_FOUND);

        public static bool IsForeignKeyViolation(DbUpdateException exception) => IsForeignKeyViolation((Exception)exception);
        public static bool IsDeadlockDetected(DbUpdateException exception) => IsDeadlockDetected((Exception)exception);
        public static bool IsDatabaseNotFound(DbUpdateException exception) => IsDatabaseNotFound((Exception)exception);
        public static bool IsCommandTimeout(DbUpdateException exception) => IsCommandTimeout((Exception)exception);
        public static bool IsConnectionFailure(DbUpdateException exception) => IsConnectionFailure((Exception)exception);

        private static bool TryExtractSqlException(Exception rootException, out SqlException extractedSqlException)
        {
            extractedSqlException = null;

            for (var currentException = rootException; currentException != null; currentException = currentException.InnerException)
            {
                if (currentException is SqlException directSqlException)
                {
                    extractedSqlException = directSqlException;
                    return true;
                }

                if (currentException is DbUpdateException dbUpdateException)
                {
                    var nestedSqlException = dbUpdateException.InnerException?.InnerException as SqlException;

                    if (nestedSqlException != null)
                    {
                        extractedSqlException = nestedSqlException;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
