using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface ILoginFaultMapper
    {
        FaultException MapLoginDbException(DbUpdateException ex);
        FaultException MapLoginException(Exception ex);
        FaultException MapLogoutDbException(DbUpdateException ex);
        FaultException MapLogoutException(Exception ex);
    }
}