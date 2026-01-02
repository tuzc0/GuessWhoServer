using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;

namespace GuessWhoServices.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false, InstanceContextMode = InstanceContextMode.PerCall)]
    public sealed class UpdateProfileService : ServiceBase, IUpdateProfileService
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(UpdateProfileService));

        private const string LOG_CTX_GET_PROFILE = "UpdateProfileService.GetProfile";
        private const string LOG_CTX_UPDATE_PROFILE = "UpdateProfileService.UpdateUserProfile";
        private const string LOG_CTX_DELETE_PROFILE = "UpdateProfileService.DeleteUserProfile";

        private const string EMPTY = "";
        private const int MIN_VALID_ID = 1;

        private readonly IUpdateProfileManager updateProfileManager;

        public UpdateProfileService(IUpdateProfileManager updateProfileManager)
        {
            this.updateProfileManager = updateProfileManager ??
                throw new ArgumentNullException(nameof(updateProfileManager));
        }

        public GetProfileResponse GetProfile(GetProfileRequest request)
        {
            return ExecuteService(
                LOG_CTX_GET_PROFILE,
                () =>
                {
                    EnsureRequestNotNull(request);

                    ProfileSnapshot snapshot = updateProfileManager.GetProfile(request.UserId);

                    return new GetProfileResponse
                    {
                        Username = snapshot.Username ?? EMPTY,
                        Email = snapshot.Email ?? EMPTY,
                        CreatedAtUtc = snapshot.CreatedAtUtc,
                        AvatarId = snapshot.AvatarId ?? EMPTY
                    };
                });
        }

        public UpdateProfileResponse UpdateUserProfile(UpdateProfileRequest request)
        {
            return ExecuteService(
                LOG_CTX_UPDATE_PROFILE,
                () =>
                {
                    EnsureRequestNotNull(request);

                    UpdateProfileArgs args = BuildUpdateArgs(request);

                    UpdatedProfileSnapshot updated = updateProfileManager.UpdateUserProfile(args);

                    return new UpdateProfileResponse
                    {
                        Updated = updated.Updated,
                        Email = updated.Email ?? EMPTY,
                        Username = updated.Username ?? EMPTY,
                        UpdatedAtUtc = updated.UpdatedAtUtc,
                        AvatarId = updated.AvatarId ?? EMPTY
                    };
                });
        }

        public BasicResponse DeleteUserProfile(DeleteProfileRequest request)
        {
            return ExecuteService(
                LOG_CTX_DELETE_PROFILE,
                () =>
                {
                    EnsureRequestNotNull(request);

                    bool success = updateProfileManager.DeleteUserProfile(request.UserId);

                    return new BasicResponse
                    {
                        Success = success
                    };
                });
        }

        private static void EnsureRequestNotNull(object request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                UpdateProfileFaultKeys.CODE_REQUEST_NULL,
                UpdateProfileFaultKeys.MSG_REQUEST_NULL,
                UpdateProfileFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static UpdateProfileArgs BuildUpdateArgs(UpdateProfileRequest request)
        {
            if (request.UserId < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(
                    UpdateProfileFaultKeys.CODE_USER_ID_INVALID,
                    UpdateProfileFaultKeys.MSG_USER_ID_INVALID,
                    UpdateProfileFaultKeys.FALLBACK_USER_ID_INVALID);
            }

            return new UpdateProfileArgs(
                userId: request.UserId,
                newDisplayName: request.NewDisplayName ?? EMPTY,
                newPasswordPlain: request.NewPasswordPlain ?? EMPTY,
                currentPasswordPlain: request.CurrentPasswordPlain ?? EMPTY,
                newAvatarId: request.NewAvatarId ?? EMPTY,
                nowUtc: DateTime.UtcNow);
        }
    }
}
