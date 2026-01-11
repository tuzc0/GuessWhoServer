using System;
using System.Collections.Generic;
using System.ServiceModel;


namespace GuessWhoServices.Infrastructure.Session
{
    public interface IOnlineUserRegistry
    {
        RegisterOrReplaceResult RegisterOrReplace(RegisterOrReplaceRequest request);

        bool IsOnline(IsOnlineRequest request);

        IReadOnlyCollection<long> GetOnlineUserIds();

        void Touch(TouchRequest request);

        void Remove(RemoveRequest request);
    }

    public sealed class OnlineRegistrySettings
    {
        public OnlineRegistrySettings(int leaseSeconds, int cleanupIntervalSeconds)
        {
            LeaseSeconds = leaseSeconds;
            CleanupIntervalSeconds = cleanupIntervalSeconds;
        }

        public int LeaseSeconds { get; }

        public int CleanupIntervalSeconds { get; }
    }

    public sealed class RegisterOrReplaceRequest
    {
        public RegisterOrReplaceRequest(long userId, IContextChannel channel)
        {
            UserId = userId;
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public long UserId { get; }

        public IContextChannel Channel { get; }
    }

    public sealed class IsOnlineRequest
    {
        public IsOnlineRequest(long userId)
        {
            UserId = userId;
        }

        public long UserId { get; }
    }

    public sealed class TouchRequest
    {
        public TouchRequest(long userId)
        {
            UserId = userId;
        }

        public long UserId { get; }
    }

    public sealed class RemoveRequest
    {
        public RemoveRequest(long userId, IContextChannel channel)
        {
            UserId = userId;
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public long UserId { get; }

        public IContextChannel Channel { get; }
    }

    public enum RegisterOrReplaceStatus
    {
        Registered = 1,
        RejectedAccountInUse = 2
    }

    public sealed class RegisterOrReplaceResult
    {
        private RegisterOrReplaceResult(RegisterOrReplaceStatus status, string sessionId, bool hasZombieCleaned)
        {
            Status = status;
            SessionId = sessionId ?? string.Empty;
            HasZombieCleaned = hasZombieCleaned;
        }

        public RegisterOrReplaceStatus Status { get; }

        public string SessionId { get; }

        public bool HasZombieCleaned { get; }

        public bool IsRegistered => Status == RegisterOrReplaceStatus.Registered;

        public static RegisterOrReplaceResult Registered(string sessionId, bool hasZombieCleaned)
        {
            return new RegisterOrReplaceResult(RegisterOrReplaceStatus.Registered, sessionId, hasZombieCleaned);
        }

        public static RegisterOrReplaceResult RejectedAccountInUse()
        {
            return new RegisterOrReplaceResult(RegisterOrReplaceStatus.RejectedAccountInUse, string.Empty, false);
        }
    }
}
