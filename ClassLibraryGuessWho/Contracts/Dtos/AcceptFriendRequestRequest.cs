using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class AcceptFriendRequestRequest
    {
        [DataMember(IsRequired = true)] public string AccountId { get; set; }
        [DataMember(IsRequired = true)] public string FriendRequestId { get; set; }
    }
}
