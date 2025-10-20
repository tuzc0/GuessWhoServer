using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class SendFriendRequestResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
        [DataMember] public string FriendRequestId { get; set; }
        [DataMember] public bool AutoAccepted { get; set; }
    }
}
