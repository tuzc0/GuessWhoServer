using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class RegisterGuestResponse : BasicResponse
    {
        [DataMember]
        public long UserId { get; set; }

        [DataMember]
        public string DisplayName { get; set; }
    }
}
