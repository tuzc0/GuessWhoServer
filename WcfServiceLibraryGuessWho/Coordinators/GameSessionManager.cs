using GuessWhoCore.Contracts.Faults;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoDataAccess.Data.Factories;

namespace GuessWhoServices.Coordinators
{
    public sealed class GameSessionManager : ManagerBase, IGameSessionManager
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(GameSessionManager));

        private const string LOG_CTX_TERMINATE = "GameSessionManager.TerminateActiveSessions";

        private const int MIN_VALID_ID = 1;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public GameSessionManager(IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public bool TerminateActiveSessions(long userId)
        {
            return ExecuteService(
                LOG_CTX_TERMINATE,
                () =>
                {
                    EnsureValidUserIdOrThrow(userId);

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    bool terminated = unitOfWork.Matches.ForceLeaveAllMatchesForUser(userId);

                    unitOfWork.Flush();
                    transaction.Commit();

                    return terminated;
                });
        }

        private static void EnsureValidUserIdOrThrow(long userId)
        {
            if (userId >= MIN_VALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(GameSessionFaultKeys.CODE_USER_ID_INVALID);
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
