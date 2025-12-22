using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Communication.Email;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface IUserFaultMapper
    {
        FaultException MapRegisterDb (DbUpdateException dbUpdateException);
        FaultException MapRegisterUnknow(Exception exception);

        FaultException MapEmailVerificationDb(DbUpdateException dbUpdateException);
        FaultException MapEmailVerificationUnknown(Exception exception);

        FaultException MapResendDb(DbUpdateException dbUpdateException);
        FaultException MapResendUnknown(Exception exception);

        FaultException MapPasswordRecoveryDb(DbUpdateException dbUpdateException);
        FaultException MapPasswordRecoveryUnknown(Exception exception);

        FaultException MapUpdatePasswordDb(DbUpdateException dbUpdateException);
        FaultException MapUpdatePasswordUnknown(Exception exception);

        FaultException MapEmailSend(EmailSendException emailSendException);
    }
}
