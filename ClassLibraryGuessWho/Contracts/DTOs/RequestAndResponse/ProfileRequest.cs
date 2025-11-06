using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class ProfileRequest
    {
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
        [DataMember(IsRequired = true)] public string email { get; set; }
        [DataMember(IsRequired = true)] public string password { get; set; }

        //Date Member avatar { get; set; }
    }
}