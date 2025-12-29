namespace GuessWhoCore.Dtos
{
    public class LobbyNotificationDto
    {
        public long MatchId { get; set; }

        public long UserId { get; set; }

        public string OperationName { get; set; }
    }
}
