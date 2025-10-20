using ClassLibraryGuessWho.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryGuessWho.Contracts.Services
{
    [ServiceContract]
    public interface IProfileService
    {
        [OperationContract]
        ProfileResponse Profile(ProfileRequest request);
    }
}