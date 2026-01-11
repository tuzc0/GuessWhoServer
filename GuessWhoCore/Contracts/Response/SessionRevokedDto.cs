using System;

namespace GuessWhoCore.Contracts.Response
{
    public sealed class SessionRevokedDto
    {
        public string MessageKey { get; set; }
        public string Reason { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
