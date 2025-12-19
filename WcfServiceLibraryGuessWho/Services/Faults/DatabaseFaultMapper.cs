using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services.ErrorHandling
{
    public class DatabaseFaultMapper
    {
        public static FaultException<ServiceFault> MapInfrastructureError(
            Exception ex,
            string operationContext, 
            ILog logger, 
            DatabaseFaultMappingOptions faultMapper)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (!errorInfo.HasSqlException)
            {
                logger.Fatal(operationContext + ": unexpected non-SQL exception.", ex);

                return Faults.Create(
                    faultMapper.UnexpectedCode,
                    faultMapper.UnexpectedMessage,
                    ex);
            }

            switch (errorInfo.Kind)
            {
                case SqlErrorKind.Timeout:

                    logger.Fatal(operationContext + ": database command timeout.", ex);

                    return Faults.Create(
                        faultMapper.ConnectionCode,
                        faultMapper.ConnectionMessage,
                        ex);

                case SqlErrorKind.ConnectionFailure:

                    logger.Fatal(operationContext + ": database connection failure." + ex);
                    
                    return Faults.Create(
                        faultMapper.ConnectionCode, 
                        faultMapper.ConnectionMessage,
                        ex);

                case SqlErrorKind.DatabaseNotFound:

                    logger.Fatal(operationContext + ": database not found." + ex);

                    return Faults.Create(
                        faultMapper.ConnectionCode,
                        faultMapper.ConnectionMessage,
                        ex);

                default:

                    logger.Fatal(operationContext + ": unclassified SQL exception.", ex);

                    return Faults.Create(
                        faultMapper.ConnectionCode,
                        faultMapper.ConnectionMessage,
                        ex);
            }
        }
    }
}
