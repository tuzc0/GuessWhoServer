using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        RegisterResponse RegisterUser(RegisterRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void ResendEmailVerificationCode(ResendVerificationRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        PasswordRecoveryResponse SendPasswordRecoveryCode(PasswordRecoveryRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request);
    }
}
