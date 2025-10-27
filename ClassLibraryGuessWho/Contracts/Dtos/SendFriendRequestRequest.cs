using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class SendFriendRequestRequest
    {
        [DataMember(IsRequired = true)] public long FromAccountId { get; set; }
        [DataMember(IsRequired = true)] public long ToUserId { get; set; }
    }
}
