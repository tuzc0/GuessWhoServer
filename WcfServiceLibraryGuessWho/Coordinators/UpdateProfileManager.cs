using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Validation;
using GuessWhoCore.Validation.ValidationDTOs;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoDataAccess.Data.Factories;

namespace GuessWhoServices.Coordinators
{
    public sealed class UpdateProfileManager : ManagerBase, IUpdateProfileManager
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(UpdateProfileManager));

        private const string LOG_CTX_GET_PROFILE = "UpdateProfileManager.GetProfile";
        private const string LOG_CTX_UPDATE_PROFILE = "UpdateProfileManager.UpdateUserProfile";
        private const string LOG_CTX_DELETE_PROFILE = "UpdateProfileManager.DeleteUserProfile";

        private const int MIN_VALID_ID = 1;
        private const string EMPTY = "";

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IPasswordHasher passwordHasher;

        public UpdateProfileManager(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IPasswordHasher passwordHasher)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.passwordHasher = passwordHasher ??
                throw new ArgumentNullException(nameof(passwordHasher));
        }

        public ProfileSnapshot GetProfile(long userId)
        {
            return ExecuteService(
                LOG_CTX_GET_PROFILE,
                () =>
                {
                    EnsureValidUserIdOrThrow(userId);

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    {
                        AccountWithProfileResult result =
                            unitOfWork.UserAccounts.GetAccountWithProfileByUserId(userId);

                        EnsureFoundOrThrow(result);

                        return new ProfileSnapshot(
                            result.Profile.DisplayName,
                            result.Account.Email,
                            result.Account.CreatedAtUtc,
                            result.Profile.AvatarId);
                    }
                });
        }

        public UpdatedProfileSnapshot UpdateUserProfile(UpdateProfileArgs args)
        {
            return ExecuteService(
                LOG_CTX_UPDATE_PROFILE,
                () =>
                {
                    EnsureArgsNotNullOrThrow(args);
                    EnsureValidUserIdOrThrow(args.UserId);

                    ProfileUpdateDraft draft = BuildProfileUpdateDraft(args);

                    ValidateDraftOrThrow(draft);

                    string newDisplayName = (args.NewDisplayName ?? EMPTY).Trim();
                    string newAvatarId = (args.NewAvatarId ?? EMPTY).Trim();
                    string newPasswordPlain = (args.NewPasswordPlain ?? EMPTY).Trim();

                    bool wantsNameChange = !string.IsNullOrWhiteSpace(newDisplayName);
                    bool wantsAvatarChange = !string.IsNullOrWhiteSpace(newAvatarId);
                    bool wantsPasswordChange = !string.IsNullOrWhiteSpace(newPasswordPlain);

                    if (!wantsNameChange && !wantsAvatarChange && !wantsPasswordChange)
                    {
                        throw FaultsFactory.Create(
                            UpdateProfileFaultKeys.CODE_NO_CHANGES_PROVIDED,
                            UpdateProfileFaultKeys.MSG_NO_CHANGES_PROVIDED,
                            UpdateProfileFaultKeys.FALLBACK_NO_CHANGES_PROVIDED);
                    }

                    var search = new AccountSearchParameters
                    {
                        UserId = args.UserId,
                        Email = EMPTY
                    };

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    AccountWithProfileResult loaded =
                        unitOfWork.UserAccounts.TryGetAccountWithProfileForUpdate(search);

                    EnsureFoundOrThrow(loaded);

                    byte[] effectivePasswordHash = loaded.Account.PasswordHash;
                    DateTime nowUtc = args.NowUtc != default ? args.NowUtc : DateTime.UtcNow;

                    if (wantsPasswordChange)
                    {
                        effectivePasswordHash = ValidateAndHashNewPasswordOrThrow(args, loaded.Account.PasswordHash);
                    }

                    var updateArgs = new UpdateAccountArgs
                    {
                        AccountId = loaded.Account.AccountId,
                        NewDisplayName = wantsNameChange ? newDisplayName : loaded.Profile.DisplayName,
                        NewPasswordHash = effectivePasswordHash,
                        UpdatedAtUtc = nowUtc,
                        NewAvatarId = wantsAvatarChange ? newAvatarId : loaded.Profile.AvatarId
                    };

                    UpdatedAccountResult updated =
                        unitOfWork.UserAccounts.UpdateDisplayNameAndPassword(updateArgs);

                    if (updated == null || !updated.IsSuccess)
                    {
                        throw FaultsFactory.Create(
                            UpdateProfileFaultKeys.CODE_UPDATE_FAILED,
                            UpdateProfileFaultKeys.MSG_UPDATE_FAILED,
                            UpdateProfileFaultKeys.FALLBACK_UPDATE_FAILED);
                    }

                    unitOfWork.Flush();
                    transaction.Commit();

                    return new UpdatedProfileSnapshot(
                        updated: true,
                        username: updated.Profile.DisplayName,
                        email: updated.Account.Email,
                        updatedAtUtc: updated.Account.UpdatedAtUtc,
                        avatarId: updated.Profile.AvatarId);
                });
        }

        public bool DeleteUserProfile(long userId)
        {
            return ExecuteService(
                LOG_CTX_DELETE_PROFILE,
                () =>
                {
                    EnsureValidUserIdOrThrow(userId);

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    AccountWithProfileResult existing =
                        unitOfWork.UserAccounts.GetAccountWithProfileByUserId(userId);

                    EnsureFoundOrThrow(existing);

                    DateTime nowUtc = DateTime.UtcNow;

                    bool success = unitOfWork.UserAccounts.DeleteAccount(userId, nowUtc);

                    if (!success)
                    {
                        throw FaultsFactory.Create(
                            UpdateProfileFaultKeys.CODE_PROFILE_DELETE_FAILED,
                            UpdateProfileFaultKeys.MSG_PROFILE_DELETE_FAILED,
                            UpdateProfileFaultKeys.FALLBACK_PROFILE_DELETE_FAILED);
                    }

                    unitOfWork.Flush();
                    transaction.Commit();

                    return true;
                });
        }

        private static void EnsureArgsNotNullOrThrow(UpdateProfileArgs args)
        {
            if (args != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                UpdateProfileFaultKeys.CODE_REQUEST_NULL,
                UpdateProfileFaultKeys.MSG_REQUEST_NULL,
                UpdateProfileFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static ProfileUpdateDraft BuildProfileUpdateDraft(UpdateProfileArgs args)
        {
            var passwordChange = new PasswordChangeDraft(
                currentPassword: args.CurrentPasswordPlain,
                newPassword: args.NewPasswordPlain);

            return new ProfileUpdateDraft(
                displayName: args.NewDisplayName,
                avatarId: args.NewAvatarId,
                passwordChange: passwordChange);
        }

        private static void ValidateDraftOrThrow(ProfileUpdateDraft draft)
        {
            IReadOnlyList<ValidationError> errors = UserRules.Validate(draft);

            if (errors == null || errors.Count == 0)
            {
                return;
            }

            string key = errors[0].Key ?? string.Empty;

            switch (key)
            {
                case "Profile.DisplayName.TooShort":
                case "Profile.DisplayName.TooLong":
                case "Profile.DisplayName.InvalidFormat":
                    throw FaultsFactory.Create(
                        UpdateProfileFaultKeys.CODE_DISPLAYNAME_INVALID,
                        UpdateProfileFaultKeys.MSG_DISPLAYNAME_INVALID,
                        UpdateProfileFaultKeys.FALLBACK_DISPLAYNAME_INVALID);

                case "Profile.Avatar.TooLong":
                    throw FaultsFactory.Create(
                        UpdateProfileFaultKeys.CODE_AVATAR_INVALID,
                        UpdateProfileFaultKeys.MSG_AVATAR_INVALID,
                        UpdateProfileFaultKeys.FALLBACK_AVATAR_INVALID);

                case "Profile.Password.CurrentRequired":
                    throw FaultsFactory.Create(
                        UpdateProfileFaultKeys.CODE_CURRENT_PASSWORD_REQUIRED,
                        UpdateProfileFaultKeys.MSG_CURRENT_PASSWORD_REQUIRED,
                        UpdateProfileFaultKeys.FALLBACK_CURRENT_PASSWORD_REQUIRED);

                case "Profile.Password.TooShort":
                case "Profile.Password.TooLong":
                    throw FaultsFactory.Create(
                        UpdateProfileFaultKeys.CODE_PASSWORD_INVALID,
                        UpdateProfileFaultKeys.MSG_PASSWORD_INVALID,
                        UpdateProfileFaultKeys.FALLBACK_PASSWORD_INVALID);

                default:
                    throw FaultsFactory.Create(
                        UpdateProfileFaultKeys.CODE_REQUEST_NULL,
                        UpdateProfileFaultKeys.MSG_REQUEST_NULL,
                        UpdateProfileFaultKeys.FALLBACK_REQUEST_NULL);
            }
        }

        private static void EnsureValidUserIdOrThrow(long userId)
        {
            if (userId >= MIN_VALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(
                UpdateProfileFaultKeys.CODE_USER_ID_INVALID,
                UpdateProfileFaultKeys.MSG_USER_ID_INVALID,
                UpdateProfileFaultKeys.FALLBACK_USER_ID_INVALID);
        }

        private static void EnsureFoundOrThrow(AccountWithProfileResult result)
        {
            if (result != null && result.IsFound)
            {
                return;
            }

            throw FaultsFactory.Create(
                UpdateProfileFaultKeys.CODE_PROFILE_NOT_FOUND,
                UpdateProfileFaultKeys.MSG_PROFILE_NOT_FOUND,
                UpdateProfileFaultKeys.FALLBACK_PROFILE_NOT_FOUND);
        }

        private byte[] ValidateAndHashNewPasswordOrThrow(UpdateProfileArgs args, byte[] currentPasswordHash)
        {
            string currentPasswordPlain = (args.CurrentPasswordPlain ?? EMPTY).Trim();

            if (string.IsNullOrWhiteSpace(currentPasswordPlain))
            {
                throw FaultsFactory.Create(
                    UpdateProfileFaultKeys.CODE_CURRENT_PASSWORD_REQUIRED,
                    UpdateProfileFaultKeys.MSG_CURRENT_PASSWORD_REQUIRED,
                    UpdateProfileFaultKeys.FALLBACK_CURRENT_PASSWORD_REQUIRED);
            }

            bool isCurrentValid = passwordHasher.VerifyPassword(currentPasswordPlain, currentPasswordHash);

            if (!isCurrentValid)
            {
                throw FaultsFactory.Create(
                    UpdateProfileFaultKeys.CODE_CURRENT_PASSWORD_INCORRECT,
                    UpdateProfileFaultKeys.MSG_CURRENT_PASSWORD_INCORRECT,
                    UpdateProfileFaultKeys.FALLBACK_CURRENT_PASSWORD_INCORRECT);
            }

            return passwordHasher.HashPassword(args.NewPasswordPlain ?? EMPTY);
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
