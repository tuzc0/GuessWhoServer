using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;

namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs
{
    public class UserFaultMapper : IUserFaultMapper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserFaultMapper));

        public FaultException MapRegisterDb(DbUpdateException dbUpdateException)
        {
            return MapDbCommon(dbUpdateException);
        }

        public FaultException MapRegisterUnknow(Exception exception)
        {
            Logger.Fatal("Unexpected error during registration.", exception);
            return Faults.Create(
                UserServiceFaults.FAULT_CODE_UNEXPECTED_ERROR,
                UserServiceFaults.FAULT_MESSAGE_UNEXPECTED_ERROR,
                exception);
        }

        public FaultException MapEmailVerificationDb(DbUpdateException dbUpdateException)
        {
            return MapDbCommon(dbUpdateException);
        }

        public FaultException MapEmailVerificationUnknown(Exception exception)
        {
            Logger.Fatal("Unexpected error during email verification.", exception);
            return Faults.Create(
                UserServiceFaults.FAULT_CODE_UNEXPECTED_ERROR,
                UserServiceFaults.FAULT_MESSAGE_UNEXPECTED_ERROR,
                exception);
        }

        public FaultException MapResendDb(DbUpdateException dbUpdateException)
        {
            return MapDbCommon(dbUpdateException);
        }

        public FaultException MapResendUnknown(Exception exception)
        {
            Logger.Fatal("Unexpected error during resend email verification.", exception);
            return Faults.Create(
                UserServiceFaults.FAULT_CODE_UNEXPECTED_ERROR,
                UserServiceFaults.FAULT_MESSAGE_UNEXPECTED_ERROR,
                exception);
        }

        public FaultException MapPasswordRecoveryDb(DbUpdateException dbUpdateException)
        {
            return MapDbCommon(dbUpdateException);
        }

        public FaultException MapPasswordRecoveryUnknown(Exception exception)
        {
            Logger.Fatal("Unexpected error during password recovery.", exception);
            return Faults.Create(
                UserServiceFaults.FAULT_CODE_UNEXPECTED_ERROR,
                UserServiceFaults.FAULT_MESSAGE_UNEXPECTED_ERROR,
                exception);
        }

        public FaultException MapUpdatePasswordDb(DbUpdateException dbUpdateException)
        {
            return MapDbCommon(dbUpdateException);
        }

        public FaultException MapUpdatePasswordUnknown(Exception exception)
        {
            Logger.Fatal("Unexpected error during password update.", exception);
            return Faults.Create(
                UserServiceFaults.FAULT_CODE_UNEXPECTED_ERROR,
                UserServiceFaults.FAULT_MESSAGE_UNEXPECTED_ERROR,
                exception);
        }

        public FaultException MapEmailSend(EmailSendException emailSendException)
        {
            Logger.Error("Error sending email.", emailSendException);
            return Faults.Create(
                emailSendException.Code,
                emailSendException.Message,
                emailSendException);
        }

        private static FaultException MapDbCommon(Exception exception)
        {
            var errorInfo = SqlExceptionInspector.Inspect(exception);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:

                        Logger.Fatal("Database command timeout", exception);

                        return Faults.Create(
                            UserServiceFaults.FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            UserServiceFaults.FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            exception);

                    case SqlErrorKind.ConnectionFailure:

                    case SqlErrorKind.DatabaseNotFound:

                        Logger.Fatal("Database connection failure ", exception);

                        return Faults.Create(
                            UserServiceFaults.FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            UserServiceFaults.FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            exception);
                }
            }

            Logger.Fatal("Unexpected database error.", exception);
            return Faults.Create(
                UserServiceFaults.FAULT_CODE_UNEXPECTED_ERROR,
                UserServiceFaults.FAULT_MESSAGE_UNEXPECTED_ERROR,
                exception);
        }
    }
}