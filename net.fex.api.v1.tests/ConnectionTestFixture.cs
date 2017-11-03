using System;
using NUnit.Framework;

namespace net.fex.api.v1.tests
{
    [TestFixture]
    public class ConnectionTestFixture
    {
        const string loginValid = "slutai";
        const string passwordValid = "100~`!@#$%^&*()[]{}:;\"',<.>/?+=-_";

        [Test]
        public void SignInSignOuTestOk()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
            {
                var user = conn.SignIn(loginValid, passwordValid, false);
                Assert.IsNotNull(user);
                Assert.AreEqual(loginValid, user.Login);

                conn.SignOut();
            }
        }

        [Test]
        public void SignInFailFakeLogin()
        {
            try
            {
                using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
                {
                    Assert.That(() => conn.SignIn("fakelogin", "fakepassword", false), Throws.TypeOf<LoginException>());
                }
            }
            catch (LoginException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Test]
        public void SignInFailFakePassword()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
            {
                Assert.That(() => conn.SignIn(loginValid, "fakepassword", false), Throws.TypeOf<LoginException>());
            }
        }

        [Test]
        public void SignInOnFakeUrl()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fake.net")))
            {
                Assert.That(() => conn.SignIn("fakelogin", "fakepassword", false), Throws.TypeOf<ConnectionException>());
            }
        }
    }
}
