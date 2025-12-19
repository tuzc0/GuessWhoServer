namespace GuessWhoContracts.Dtos.Dto
{
    public class LobbyNotificationDto
    {
        public long MatchId { get; set; }

        public long UserId { get; set; }

        public string OperationName { get; set; }
    }
}
