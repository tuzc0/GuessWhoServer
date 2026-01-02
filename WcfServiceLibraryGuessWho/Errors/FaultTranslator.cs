using GuessWhoCore.Contracts.Faults;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.ServiceModel;

namespace GuessWhoServices.Errors
{
    public static class FaultTranslator
    {
        private static class SqlErrorCodes
        {
            public const int NETWORK_PATH_NOT_FOUND = 53;
            public const int SERVER_NOT_FOUND = 2;
            public const int TRANSPORT_LEVEL_ERROR = 233;

            public const int CANNOT_OPEN_DATABASE = 4060;
            public const int UNABLE_TO_OPEN_PHYSICAL_FILE = 5120;

            public const int LOGIN_FAILED = 18456;
            public const int SQL_TIMEOUT_NUMBER = -2;

            public const int UNIQUE_CONSTRAIN_VIOLATION = 2627;
            public const int UNIQUE_INDEX_VIOLATION = 2601;
        }

        public static bool IsUniqueConstraintViolation(Exception ex)
        {
            SqlException code = TryGetSqlException(ex);

            if (code != null)
            {
                return false;
            }

            return code.Number == SqlErrorCodes.UNIQUE_CONSTRAIN_VIOLATION || 
                   code.Number == SqlErrorCodes.UNIQUE_INDEX_VIOLATION;
        }

        public static FaultException<ServiceFault> ToTechnicalFault(Exception ex, ILog logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (ex == null)
            {
                logger.Error("Unexpected error: exception is null.");

                return FaultsFactory.Create(
                    InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR,
                    InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR,
                    InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
            }

            SqlException sqlEx = TryGetSqlException(ex);

            if (sqlEx != null)
            {
                if (IsTimeout(sqlEx))
                {
                    logger.Error("Database operation timeout.", sqlEx);

                    return FaultsFactory.Create(
                        InfrastructureFaultKeys.CODE_DATABASE_COMMAND_TIMEOUT,
                        InfrastructureFaultKeys.MSG_DATABASE_COMMAND_TIMEOUT,
                        InfrastructureFaultKeys.FALLBACK_DATABASE_COMMAND_TIMEOUT,
                        sqlEx);
                }

                if (IsDatabaseMissing(sqlEx))
                {
                    logger.Fatal("Database does not exist or cannot be opened.", sqlEx);

                    return FaultsFactory.Create(
                        InfrastructureFaultKeys.CODE_DATABASE_NOT_FOUND,
                        InfrastructureFaultKeys.MSG_DATABASE_NOT_FOUND,
                        InfrastructureFaultKeys.FALLBACK_DATABASE_NOT_FOUND,
                        sqlEx);
                }

                FaultException<ServiceFault> unreachableFault = TryTranslateUnreachable(sqlEx, logger);

                if (unreachableFault != null)
                {
                    return unreachableFault;
                }

                if (IsPhysicalStorageError(sqlEx))
                {
                    logger.Error("Physical storage error detected.", sqlEx);

                    return FaultsFactory.Create(
                        InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR,
                        InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR,
                        InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR,
                        sqlEx);
                }

                if (sqlEx.Number == SqlErrorCodes.LOGIN_FAILED)
                {
                    logger.Fatal("Database login failed (server configuration).", sqlEx);

                    return FaultsFactory.Create(
                        InfrastructureFaultKeys.CODE_DATABASE_CONNECTION_FAILURE,
                        InfrastructureFaultKeys.MSG_DATABASE_CONNECTION_FAILURE,
                        InfrastructureFaultKeys.FALLBACK_DATABASE_CONNECTION_FAILURE,
                        sqlEx);
                }
            }

            logger.Error("Unexpected server error.", ex);

            return FaultsFactory.Create(
                InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR,
                ex);
        }

        private static FaultException<ServiceFault> TryTranslateUnreachable(SqlException sqlEx, ILog logger)
        {
            if (sqlEx.Number == SqlErrorCodes.NETWORK_PATH_NOT_FOUND)
            {
                logger.Fatal("Database network path not found.", sqlEx);

                return FaultsFactory.Create(
                    InfrastructureFaultKeys.CODE_DATABASE_NETWORK_PATH_NOT_FOUND,
                    InfrastructureFaultKeys.MSG_DATABASE_NETWORK_PATH_NOT_FOUND,
                    InfrastructureFaultKeys.FALLBACK_DATABASE_NETWORK_PATH_NOT_FOUND,
                    sqlEx);
            }

            if (sqlEx.Number == SqlErrorCodes.SERVER_NOT_FOUND)
            {
                logger.Fatal("Database server not found.", sqlEx);

                return FaultsFactory.Create(
                    InfrastructureFaultKeys.CODE_DATABASE_SERVER_NOT_FOUND,
                    InfrastructureFaultKeys.MSG_DATABASE_SERVER_NOT_FOUND,
                    InfrastructureFaultKeys.FALLBACK_DATABASE_SERVER_NOT_FOUND,
                    sqlEx);
            }

            if (sqlEx.Number == SqlErrorCodes.TRANSPORT_LEVEL_ERROR)
            {
                logger.Fatal("Database transport-level error.", sqlEx);

                return FaultsFactory.Create(
                    InfrastructureFaultKeys.CODE_DATABASE_TRANSPORT_LEVEL_ERROR,
                    InfrastructureFaultKeys.MSG_DATABASE_TRANSPORT_LEVEL_ERROR,
                    InfrastructureFaultKeys.FALLBACK_DATABASE_TRANSPORT_LEVEL_ERROR,
                    sqlEx);
            }

            return null;
        }

        private static SqlException TryGetSqlException(Exception ex)
        {
            if (ex is SqlException directSql)
            {
                return directSql;
            }

            if (ex is DbUpdateException dbUpdateException)
            {
                return FindSqlException(dbUpdateException);
            }

            return FindSqlException(ex);
        }

        private static SqlException FindSqlException(Exception ex)
        {
            Exception current = ex;

            while (current != null)
            {
                if (current is SqlException sqlEx)
                {
                    return sqlEx;
                }

                current = current.InnerException;
            }

            return null;
        }

        private static bool IsTimeout(SqlException ex)
        {
            return ex.Number == SqlErrorCodes.SQL_TIMEOUT_NUMBER;
        }

        private static bool IsDatabaseMissing(SqlException ex)
        {
            return ex.Number == SqlErrorCodes.CANNOT_OPEN_DATABASE;
        }

        private static bool IsPhysicalStorageError(SqlException ex)
        {
            return ex.Number == SqlErrorCodes.UNABLE_TO_OPEN_PHYSICAL_FILE;
        }
    }
}
