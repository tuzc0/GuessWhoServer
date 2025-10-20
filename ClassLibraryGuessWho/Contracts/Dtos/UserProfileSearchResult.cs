using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class UserProfileSearchResult
    {
        [DataMember(IsRequired = true)] public string UserId { get; set; }
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
        [DataMember(IsRequired = true)] public string AvatarUrl { get; set; }
    }
}
