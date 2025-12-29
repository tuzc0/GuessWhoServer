using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class GetMatchDeckRequest
    {
        [DataMember]
        public long MatchId { get; set; }

        [DataMember]
        public int NumberOfCardsInDeck { get; set; }
    }
}
