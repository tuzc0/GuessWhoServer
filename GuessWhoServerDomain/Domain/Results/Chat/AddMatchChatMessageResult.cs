using GuessWhoServerDomain.Domain.Enums.Chat;

namespace GuessWhoServerDomain.Domain.Results.Chat
{
    public readonly record struct AddMatchChatMessageResult(AddMatchChatMessageResultCode Code)
    {
        public bool IsSuccess => Code == AddMatchChatMessageResultCode.Success;

        public static AddMatchChatMessageResult Success() => new(AddMatchChatMessageResultCode.Success);

        public static AddMatchChatMessageResult Fail(AddMatchChatMessageResultCode code) => new(code);
    }
}
