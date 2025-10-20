using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class BasicResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
    }
}
