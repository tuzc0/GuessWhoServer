using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class PasswordRecoveryResponse
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string MessageCode { get; set; }
        [DataMember] public string Message { get; set; }
    }
}