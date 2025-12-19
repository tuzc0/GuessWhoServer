using GuessWhoContracts.Enums;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace ClassLibraryGuessWho.Data.Helpers
{
    public class SqlErrorInfo
    {
        public bool HasSqlException { get; }
        public SqlErrorKind Kind { get; }
        public SqlException SqlException { get; }

        public SqlErrorInfo(bool hasSqlException, SqlErrorKind kind, SqlException sqlException)
        {
            HasSqlException = hasSqlException;
            Kind = kind;
            SqlException = sqlException;
        }
    }

    public static class SqlExceptionInspector
    {
        private const int ERROR_FOREIGN_KEY_VIOLATION = 547;
        private const int ERROR_DEADLOCK = 1205;
        private const int ERROR_DATABASE_NOT_FOUND = 4060;
        private const int ERROR_TIMEOUT_EXPIRED = -2;
        private const int ERROR_CONNECTION_FAILURE = -1;
        private const int ERROR_SERVER_NOT_FOUND = 53;
        private const int ERROR_UNIQUE_INDEX_VIOLATION = 2601;
        private const int ERROR_DUPLICATE_KEY_VIOLATION = 2627;
        private const int ERROR_LOGIN_FAILED = 18456;

        private static SqlException ExtractSqlException(Exception rootException)
        {
            for (Exception currentException = rootException;
                 currentException != null;
                 currentException = currentException.InnerException)
            {
                if (currentException is SqlException directSqlException)
                {
                    return directSqlException;
                }

                if (currentException is DbUpdateException dbUpdateException
                    && dbUpdateException.InnerException?.InnerException is
                    SqlException nestedSqlException)
                {
                    return nestedSqlException;
                }
            }

            return null;
        }

        public static SqlErrorInfo Inspect(Exception exception)
        {
            var sqlException = ExtractSqlException(exception);

            if (sqlException == null)
            {
                return new SqlErrorInfo(false, SqlErrorKind.None, sqlException);
            }

            SqlErrorKind kind; 

            switch (sqlException.Number)
            {
                case ERROR_FOREIGN_KEY_VIOLATION:

                    kind = SqlErrorKind.ForeignKeyViolation;
                    break;

                case ERROR_UNIQUE_INDEX_VIOLATION:
                case ERROR_DUPLICATE_KEY_VIOLATION:
                    
                    kind = SqlErrorKind.ForeignKeyViolation;
                    break;

                case ERROR_DEADLOCK:

                    kind = SqlErrorKind.Deadlock;
                    break;

                case ERROR_DATABASE_NOT_FOUND:

                    kind = SqlErrorKind.DatabaseNotFound;
                    break;

                case ERROR_TIMEOUT_EXPIRED:

                    kind = SqlErrorKind.Timeout;
                    break;

                case ERROR_CONNECTION_FAILURE:
                case ERROR_SERVER_NOT_FOUND:

                    kind = SqlErrorKind.ConnectionFailure;
                    break;

                case ERROR_LOGIN_FAILED:

                    kind = SqlErrorKind.LoginFailed;
                    break;

                default:

                    kind = SqlErrorKind.None;
                    break;
            }

            return new SqlErrorInfo(true, kind, sqlException);
        }
    }
}