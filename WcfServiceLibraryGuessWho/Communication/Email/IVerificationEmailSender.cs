using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WcfServiceLibraryGuessWho.Communication.Email
{
    public interface IVerificationEmailSender
    {
        void SendVerificationCode(string recipientEmailAddress, string verificationCode);
    }
}
