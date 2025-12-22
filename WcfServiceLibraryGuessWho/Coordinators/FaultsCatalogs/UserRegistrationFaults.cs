namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs
{
    internal static class UserRegistrationFaults
    {
        internal const int VERIFICATION_CODE_EXPIRY_MINUTES = 10;

        internal const string ERROR_MESSAGE_ARGS_REQUIRED = "Registration data is required.";
        internal const string ERROR_MESSAGE_EMAIL_REQUIRED = "Email must not be empty.";
        internal const string ERROR_MESSAGE_EMAIL_ALREADY_EXISTS = "A user with this email already exists.";
        internal const string ERROR_MESSAGE_PASSWORD_REQUIRED = "Password must not be empty.";
        internal const string ERROR_MESSAGE_DISPLAYNAME_REQUIRED = "Display name must not be empty.";
        internal const string ERROR_MESSAGE_TOKEN_CREATION_FAILED = "Could not create email verification token.";
        internal const string ERROR_MESSAGE_NOWUTC_REQUIRED = "Registration timestamp is required.";
    }
}
