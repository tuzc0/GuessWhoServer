using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;

namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs.Mappers
{
    public class LoginFaultMapper : ILoginFaultMapper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginFaultMapper));

        public FaultException MapLoginDbException(DbUpdateException dbUpdateException)
        {
            return MapDbCommon(dbUpdateException);
        }

        public FaultException MapLoginException(Exception exception)
        {
            string message = exception.Message;
            string code = LoginServiceFaults.FAULT_CODE_LOGIN_UNEXPECTED_ERROR;

            if (message == LoginServiceFaults.ERROR_MESSAGE_ACCOUNT_LOCKED)
            {
                code = LoginServiceFaults.FAULT_CODE_LOGIN_ACCOUNT_LOCKED;
            }
            else if (message == LoginServiceFaults.ERROR_MESSAGE_INVALID_PASSWORD ||
                     message == LoginServiceFaults.ERROR_MESSAGE_ACCOUNT_NOT_FOUND)
            {
                code = LoginServiceFaults.FAULT_CODE_LOGIN_INVALID_CREDENTIALS;
            }
            else if (message == LoginServiceFaults.ERROR_MESSAGE_PROFILE_ALREADY_ACTIVE)
            {
                code = LoginServiceFaults.FAULT_CODE_LOGIN_PROFILE_ALREADY_ACTIVE;
            }
            else
            {
                Logger.Fatal("Unexpected error during login process logic.", exception);
                return Faults.Create(
                    LoginServiceFaults.FAULT_CODE_LOGIN_UNEXPECTED_ERROR,
                    LoginServiceFaults.FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR,
                    exception);
            }

            return Faults.Create(code, message, exception);
        }

        public FaultException MapLogoutDbException(DbUpdateException dbUpdateException)
        {
            return MapDbCommon(dbUpdateException);
        }

        public FaultException MapLogoutException(Exception exception)
        {
            Logger.Error("Error during logout process.", exception);
            return Faults.Create(
                LoginServiceFaults.FAULT_CODE_LOGOUT_FAILED,
                LoginServiceFaults.FAULT_MESSAGE_LOGOUT_FAILED,
                exception);
        }

        private static FaultException MapDbCommon(Exception exception)
        {
            var errorInfo = SqlExceptionInspector.Inspect(exception);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal("Database command timeout during login/logout", exception);
                        return Faults.Create(
                            LoginServiceFaults.FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            LoginServiceFaults.FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            exception);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal("Database connection failure during login/logout", exception);
                        return Faults.Create(
                            LoginServiceFaults.FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            LoginServiceFaults.FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            exception);
                }
            }

            Logger.Fatal("Unexpected database error in LoginService.", exception);
            return Faults.Create(
                LoginServiceFaults.FAULT_CODE_LOGIN_UNEXPECTED_ERROR,
                LoginServiceFaults.FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR,
                exception);
        }
    }
}