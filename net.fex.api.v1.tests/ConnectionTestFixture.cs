using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace net.fex.api.v1.tests
{
    [TestClass]
    public class ConnectionTestFixture
    {
        const string loginValid = "slutai";
        const string passwordValid = "100~`!@#$%^&*()[]{}:;\"',<.>/?+=-_";

        [TestMethod]
        public async Task SignInSignOuTestOk()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
            {
                var user = await conn.SignInAsync(loginValid, passwordValid, false);
                Assert.IsNotNull(user);
                Assert.AreEqual(loginValid, user.Login);

                await conn.SignOutAsync();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Connection.LoginException))]
        public async Task SignInFailFakeLogin()
        {
            try
            {
                using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
                {
                    await conn.SignInAsync("fakelogin", "fakepassword", false);
                }

            }
            catch (Connection.LoginException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Connection.LoginException))]
        public async Task SignInFailFakePassword()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
            {
                await conn.SignInAsync(loginValid, "fakepassword", false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Connection.ConnectionException))]
        public async Task SignInOnFakeUrl()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fake.net")))
            {
                var user = await conn.SignInAsync("fakelogin", "fakepassword", false);
            }
        }
    }
}
