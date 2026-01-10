using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class TournamentPlayerDto
    {
        [DataMember]
        public long UserId { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public long TournamentId { get; set; }
    }
}
