using GuessWhoServices.Infrastructure;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWhoServices.Session
{
    public interface IClientSessionRevoker
    {
        int RevokeUser(long userId, string reason);
    }

    public static class ClientSessionRevokerConstants
    {
        public const long INVALID_USER_ID = 0;
        public const int NO_SESSIONS_REVOKED = 0;

        public const string EMPTY = "";
        public const string LOG_CTX_REVOKE = "ClientSessionRevoker.RevokeUser";
        public const string LOG_INVALID_INPUTS = "ClientSessionRevoker.RevokeUser: invalid inputs. userId={0}.";
        public const string LOG_REVOKE_FAILED = "ClientSessionRevoker.RevokeUser failed. userId={0} reason='{1}'.";
    }

    public sealed class ClientSessionRevoker : IClientSessionRevoker
    {
        private readonly ILobbySubscriptionStore lobbySubscriptionStore;
        private readonly ILog logger;

        public sealed class ClientSessionRevokerArgs
        {
            public ILobbySubscriptionStore LobbySubscriptionStore { get; init; }
            public ILog Logger { get; init; }
        }

        public ClientSessionRevoker(ClientSessionRevokerArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            lobbySubscriptionStore = args.LobbySubscriptionStore ??
                throw new ArgumentNullException(nameof(args.LobbySubscriptionStore));

            logger = args.Logger ??
                throw new ArgumentNullException(nameof(args.Logger));
        }

        public int RevokeUser(long userId, string reason)
        {
            if (userId <= ClientSessionRevokerConstants.INVALID_USER_ID)
            {
                logger.WarnFormat(ClientSessionRevokerConstants.LOG_INVALID_INPUTS, userId);
                return ClientSessionRevokerConstants.NO_SESSIONS_REVOKED;
            }

            string safeReason = (reason ?? ClientSessionRevokerConstants.EMPTY).Trim();

            try
            {
                return lobbySubscriptionStore.ForceDisconnectUser(userId, safeReason);
            }
            catch (CommunicationException ex)
            {
                logger.ErrorFormat(ClientSessionRevokerConstants.LOG_REVOKE_FAILED, userId, safeReason);
                logger.Debug(ClientSessionRevokerConstants.LOG_CTX_REVOKE, ex);
                return ClientSessionRevokerConstants.NO_SESSIONS_REVOKED;
            }
            catch (TimeoutException ex)
            {
                logger.ErrorFormat(ClientSessionRevokerConstants.LOG_REVOKE_FAILED, userId, safeReason);
                logger.Debug(ClientSessionRevokerConstants.LOG_CTX_REVOKE, ex);
                return ClientSessionRevokerConstants.NO_SESSIONS_REVOKED;
            }
            catch (ObjectDisposedException ex)
            {
                logger.ErrorFormat(ClientSessionRevokerConstants.LOG_REVOKE_FAILED, userId, safeReason);
                logger.Debug(ClientSessionRevokerConstants.LOG_CTX_REVOKE, ex);
                return ClientSessionRevokerConstants.NO_SESSIONS_REVOKED;
            }
            catch (InvalidOperationException ex)
            {
                logger.ErrorFormat(ClientSessionRevokerConstants.LOG_REVOKE_FAILED, userId, safeReason);
                logger.Debug(ClientSessionRevokerConstants.LOG_CTX_REVOKE, ex);
                return ClientSessionRevokerConstants.NO_SESSIONS_REVOKED;
            }
        }
    }
}
