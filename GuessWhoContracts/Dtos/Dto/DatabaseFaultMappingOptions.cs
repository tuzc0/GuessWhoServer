namespace GuessWhoContracts.Dtos.Dto
{
    public class DatabaseFaultMappingOptions
    {
        public string UnexpectedCode { get; set; }
        public string UnexpectedMessage { get; set; }
        public string TimeoutCode { get; set; }
        public string TimeoutMessage { get; set; }
        public string ConnectionCode { get; set; }
        public string ConnectionMessage { get; set; }
    }
}
