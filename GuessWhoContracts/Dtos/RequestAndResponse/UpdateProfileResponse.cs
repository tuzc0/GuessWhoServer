using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    public sealed class UpdateProfileResponse
    {
        public bool Updated { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public string AvatarURL { get; set; }
    }
}
