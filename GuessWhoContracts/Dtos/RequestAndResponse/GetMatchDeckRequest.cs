using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
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
