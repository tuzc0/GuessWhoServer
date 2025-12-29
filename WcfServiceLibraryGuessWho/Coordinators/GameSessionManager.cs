using GuessWhoCore.Contracts.Faults;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Errors;
namespace WcfServiceLibraryGuessWho.Coordinators { 
    public sealed class GameSessionManager : ManagerBase, IGameSessionManager { 
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(GameSessionManager)); 
        
        private const string LOG_CTX_TERMINATE = "GameSessionManager.TerminateActiveSessions"; 
        
        private const int MIN_VALID_ID = 1;
        
        private readonly IGameSessionRepository sessionRepository; 
        public GameSessionManager(IGameSessionRepository sessionRepository) 
        { 
            this.sessionRepository = sessionRepository ?? 
                throw new ArgumentNullException(nameof(sessionRepository)); 
        } 
        
        public bool TerminateActiveSessions(long userId) 
        { 
            return ExecuteService(LOG_CTX_TERMINATE, () => 
            {
                if (userId < MIN_VALID_ID) 
                { return false; 
                } 
                return sessionRepository.ForceLeaveActiveSessionsForUser(userId); 
            }); 
        }
        
        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex) 
        { 
            return FaultTranslator.ToTechnicalFault(ex, Logger); 
        } 
    } 
}