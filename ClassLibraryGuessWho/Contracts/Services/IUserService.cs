using ClassLibraryGuessWho.Contracts.Dtos;
using GuessWho.Contracts.Dtos;
using System.ServiceModel;

namespace GuessWho.Contracts.Services
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
    }
}