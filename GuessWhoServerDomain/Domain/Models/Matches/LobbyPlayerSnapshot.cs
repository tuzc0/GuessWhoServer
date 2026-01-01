namespace GuessWhoServerDomain.Domain.Models.Match
{
    public readonly record struct LobbyPlayerSnapshot(
        long MatchId, 
        long UserId, 
        string DisplayName,
        string AvatarId, 
        byte SlotNumber, 
        bool IsReady, 
        bool IsHost)
    {
        public static LobbyPlayerSnapshot Create(
            long matchId, 
            long userId, 
            string displayName, 
            string avatarId, 
            byte slotNumber, 
            bool isReady, 
            bool isHost
        )
        {
            return new LobbyPlayerSnapshot(
                matchId,
                userId,
                displayName,
                avatarId,
                slotNumber,
                isReady,
                isHost);
        }
    }
}
