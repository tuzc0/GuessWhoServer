using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class RegisterGuestRequest
    {
        [DataMember]
        public string DisplayName { get; set; }
    }
}
