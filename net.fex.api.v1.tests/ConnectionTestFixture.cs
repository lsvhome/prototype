using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Net.Fex.Api;

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
                Assert.IsTrue(conn.LoginCheck(loginValid));

                Assert.IsFalse(conn.IsSignedIn);

                var user = await conn.SignInAsync(loginValid, passwordValid, false);
                Assert.IsNotNull(user);
                Assert.AreEqual(loginValid, user.Login);

                Assert.IsTrue(conn.IsSignedIn);

                await conn.SignOutAsync();

                Assert.IsFalse(conn.IsSignedIn);

                Assert.IsTrue(conn.LoginCheck(loginValid));
            }
        }

        [TestMethod]
        public async Task SignUp()
        {
            var r = new Random();
            int[] numbers = new int[7];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = r.Next(0, 9);
            }

            //string phone = "38068" + string.Join("", numbers.Select(digit => digit.ToString()));
            string phone = "380681111111";

            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
            {
                await conn.SignUpStep01Async(phone);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SignInException))]
        public async Task SignInFailFakeLogin()
        {
            try
            {
                using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
                {
                    await conn.SignInAsync("fakelogin", "fakepassword", false);
                }

            }
            catch (SignInException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SignInException))]
        public async Task SignInFailFakePassword()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fex.net")))
            {
                await conn.SignInAsync(loginValid, "fakepassword", false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConnectionException))]
        public async Task SignInOnFakeUrl()
        {
            using (var conn = new net.fex.api.v1.Connection(new Uri("https://fake.net")))
            {
                var user = await conn.SignInAsync("fakelogin", "fakepassword", false);
            }
        }
    }
}
