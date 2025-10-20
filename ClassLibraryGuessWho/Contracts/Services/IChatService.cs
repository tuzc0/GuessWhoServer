﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Services
{
    [ServiceContract(CallbackContract = typeof(IChatServiceCallback))]
    public interface IChatService
    {
        [OperationContract(IsOneWay = true)]
        void Join(string user);

        [OperationContract(IsOneWay = true)]
        void SendMessage(string user, string message);

        [OperationContract(IsOneWay = true)]
        void Leave(string user);
    }
}
