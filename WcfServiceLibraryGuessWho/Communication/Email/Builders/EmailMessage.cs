namespace WcfServiceLibraryGuessWho.Communication.Email.Builders
{
    public sealed class EmailMessage
    {
        public EmailMessage(string recipient, string subject, string body, bool isBodyHtml)
        {
            Recipient = (recipient ?? string.Empty).Trim();
            Subject = subject ?? string.Empty;
            Body = body ?? string.Empty;
            IsBodyHtml = isBodyHtml;
        }

        public string Recipient { get; }
        public string Subject { get; }
        public string Body { get; }
        public bool IsBodyHtml { get; }
    }
}
