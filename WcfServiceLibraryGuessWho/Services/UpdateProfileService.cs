using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services
{
    public class UpdateProfileService : IUpdateProfileService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginService));
        private readonly UserAccountData userAccountData = new UserAccountData();

        public GetProfileResponse GetProfile(GetProfileRequest request)
        {
            if (request == null) throw new FaultException("Invalid request.");

            long idAccount = request.IdAccount;
            if (idAccount <= 0) throw new FaultException("Invalid account id.");

            try
            {
                bool found = userAccountData.GetAccountWithProfileByIdAccount(
                    idAccount,
                    out AccountDto account,
                    out UserProfileDto profile);

                if (!found)
                {
                    Logger.Warn("View Profile failed: account not found for id '{idAccount}'.");
                    throw new FaultException("Profile not found.");
                }

                return new GetProfileResponse
                {
                    Username = profile.DisplayName,
                    Email = account.Email,
                    CreateAtUtc = account.CreatedAtUtc,
                    AvatarURL = "" 
                };
            }
            catch (FaultException) 
            { 
                throw; 
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in GetProfile.", ex);
                throw new FaultException("Unexpected server error.");
            }
        }

        public UpdateProfileResponse UpdateUserProfile(UpdateProfileRequest request)
        {
            if (request == null)
            {
                throw new FaultException("Invalid request.");
            }

            if (request.AccountId <= 0)
            {
                throw new FaultException("Invalid account id.");
            }

            bool wantsNameChange = !string.IsNullOrWhiteSpace(request.NewDisplayName);
            bool wantsPasswordChange = !string.IsNullOrWhiteSpace(request.NewPasswordPlain);

            if (!wantsNameChange && !wantsPasswordChange)
            {
                throw new FaultException("No changes provided.");
            }

            try
            {
                
                if (!userAccountData.GetAccountWithProfileByIdAccount(
                        request.AccountId,
                        out AccountDto account,
                        out UserProfileDto profile))
                {
                    throw new FaultException("Profile not found.");
                }

                byte[] newPasswordHash = null;

                if (wantsPasswordChange)
                {
                    if (string.IsNullOrWhiteSpace(request.CurrentPasswordPlain))
                    {
                        throw new FaultException("Current password required.");
                    }

                    if (!PasswordHasher.Verify(request.CurrentPasswordPlain, account.PasswordHash))
                    {
                        throw new FaultException("Current password is incorrect.");
                    }
                    
                    newPasswordHash = PasswordHasher.HashPassword(request.NewPasswordPlain);
                }

                var args = new UpdateAccountArgs
                {
                    AccountId = request.AccountId,                 
                    NewDisplayName = wantsNameChange ? request.NewDisplayName.Trim() : profile.DisplayName,
                    NewPassword = wantsPasswordChange ? newPasswordHash : account.PasswordHash,
                    UpdatedAtUtc = DateTime.UtcNow                   
                };

                var (updatedAccount, updatedProfile) = userAccountData.UpdateDisplayNameAndPassword(args);

                return new UpdateProfileResponse
                {
                    Updated = true,
                    Email = updatedAccount.Email,
                    Username = updatedProfile.DisplayName,
                    UpdatedAtUtc = updatedAccount.UpdatedAtUtc
                };
            }
            catch (FaultException) 
            { 
                throw; 
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in UpdateUserProfile.", ex);
                throw new FaultException("Unexpected server error.");
            }
        }
    }
}
