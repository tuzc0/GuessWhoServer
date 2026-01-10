using GuessWhoContracts.Dtos.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoCore.Contracts.Response
{
    public class GetLeaderboardResponse
    {
            public List<LeaderboardPlayerDto> Players { get; set; }
            public LeaderboardPlayerDto CurrentUserStats { get; set; }

            public GetLeaderboardResponse()
            {
                Players = new List<LeaderboardPlayerDto>();
            }
        }
    }

