using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    public class ProfileResponse
    {
        [DataMember(IsRequired = true)] public string username { get; set; }
        [DataMember(IsRequired = true)] public string email { get; set; }
        [DataMember(IsRequired = true)] public string password { get; set; }
    }
}
