using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Contracts.Services;
using GuessWho.Services.Security;
using System;

namespace WcfServiceLibraryGuessWho.Services
{
    public class MatchService : IMatchService
    {

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "Registration request cannot be null.");
            }

            var dateNow = DateTime.UtcNow;
            var visibilityDefaut = 1;
            var modeDefault = "Classic";
            var hostUserId = request.ProfileId;
            
            try
            {
                var code = GenerateNumericCode();
            }

        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            throw new System.NotImplementedException();
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            throw new System.NotImplementedException();
        }
        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            throw new System.NotImplementedException();
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
