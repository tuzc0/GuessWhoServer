using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using GuessWhoServices.Services.Configuration;

namespace GuessWhoTests.Services.Configuration
{
    [TestClass]
    public class SmtpSettingsTests
    {
        private const string VAL_HOST = "smtp.test.com";
        private const string VAL_USER = "admin_user";
        private const string VAL_PASS = "secure_pass_123";
        private const string VAL_FROM = "no-reply@guesswho.com";
        private const int VAL_PORT = 587;
        private const int VAL_PORT_ZERO = 0;
        private const int VAL_PORT_NEG = -1;
        private const string VAL_EMPTY = "";
        private const string VAL_WHITE = " ";
        private const string FIELD_HOST = "Host";
        private const string FIELD_PORT = "Port";
        private const string FIELD_USER = "User";
        private const string FIELD_PASS = "Password";
        private const string FIELD_FROM = "FromAddress";

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestValidate_HostIsWhitespace_ShouldThrowException()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_WHITE };

            settings.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestValidate_UserIsNull_ShouldThrowException()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, User = null };

            settings.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestValidate_PasswordIsEmpty_ShouldThrowException()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, User = VAL_USER, Password = VAL_EMPTY };

            settings.Validate();
        }

        [TestMethod]
        public void TestValidate_ConfigurationIsComplete_ShouldNotThrowException()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, User = VAL_USER, Password = VAL_PASS };

            settings.Validate();
        }

        [TestMethod]
        public void TestTryValidate_HostIsNull_ShouldReturnFalse()
        {
            SmtpSettings settings = new SmtpSettings { Host = null };

            bool result = settings.TryValidate(out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryValidate_PortIsZero_ShouldReturnFalse()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, Port = VAL_PORT_ZERO };

            bool result = settings.TryValidate(out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryValidate_UserIsWhitespace_ShouldReturnFalse()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, Port = VAL_PORT, User = VAL_WHITE };

            bool result = settings.TryValidate(out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryValidate_PasswordIsNull_ShouldReturnFalse()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, Port = VAL_PORT, User = VAL_USER, Password = null };

            bool result = settings.TryValidate(out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryValidate_FromAddressIsEmpty_ShouldReturnFalse()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, Port = VAL_PORT, User = VAL_USER, Password = VAL_PASS, FromAddress = VAL_EMPTY };

            bool result = settings.TryValidate(out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestTryValidate_HostIsMissing_ShouldIdentifyHostField()
        {
            SmtpSettings settings = new SmtpSettings { Host = null };

            settings.TryValidate(out string missingField);

            Assert.AreEqual(FIELD_HOST, missingField);
        }

        [TestMethod]
        public void TestTryValidate_PortIsNegative_ShouldIdentifyPortField()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, Port = VAL_PORT_NEG };

            settings.TryValidate(out string missingField);

            Assert.AreEqual(FIELD_PORT, missingField);
        }

        [TestMethod]
        public void TestTryValidate_UserIsMissing_ShouldIdentifyUserField()
        {
            SmtpSettings settings = new SmtpSettings { Host = VAL_HOST, Port = VAL_PORT, User = VAL_EMPTY };

            settings.TryValidate(out string missingField);

            Assert.AreEqual(FIELD_USER, missingField);
        }

        [TestMethod]
        public void TestTryValidate_SettingsAreCorrect_ShouldReturnTrue()
        {
            SmtpSettings settings = new SmtpSettings
            {
                Host = VAL_HOST,
                Port = VAL_PORT,
                User = VAL_USER,
                Password = VAL_PASS,
                FromAddress = VAL_FROM
            };

            bool result = settings.TryValidate(out _);

            Assert.IsTrue(result);
        }
    }
}