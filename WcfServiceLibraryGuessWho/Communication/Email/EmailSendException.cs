using System;

namespace WcfServiceLibraryGuessWho.Communication.Email
{
    public sealed class EmailSendException : Exception
    {
        public string Code { get; }

        public EmailSendException(string code, string message, Exception inner = null)
            : base(message, inner)
        {
            Code = code ?? "EMAIL_SEND_FAILED";
        }
    }
}
