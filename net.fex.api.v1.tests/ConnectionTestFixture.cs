using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Net.Fex.Api;

namespace Net.Fex.Api.Testss
{
    [TestClass]
    public class ConnectionTestFixture
    {
        private const string LoginValid = "slutai";
        private const string PasswordValid = "100~`!@#$%^&*()[]{}:;\"',<.>/?+=-_";

        [TestMethod]
        public async Task SignInSignOuTestOk()
        {
            using (var conn = new Net.Fex.Api.Connection(new Uri("https://fex.net")))
            {
                Assert.IsTrue(conn.LoginCheck(LoginValid));

                Assert.IsFalse(conn.IsSignedIn);

                var user = await conn.SignInAsync(LoginValid, PasswordValid, false);
                Assert.IsNotNull(user);
                Assert.AreEqual(LoginValid, user.Login);

                Assert.IsTrue(conn.IsSignedIn);

                await conn.SignOutAsync();

                Assert.IsFalse(conn.IsSignedIn);

                Assert.IsTrue(conn.LoginCheck(LoginValid));
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

            string phone = "38068" + string.Join(string.Empty, numbers.Select(digit => digit.ToString()));

            using (var conn = new Net.Fex.Api.Connection(new Uri("https://fex.net")))
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
                using (var conn = new Net.Fex.Api.Connection(new Uri("https://fex.net")))
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
            using (var conn = new Net.Fex.Api.Connection(new Uri("https://fex.net")))
            {
                await conn.SignInAsync(LoginValid, "fakepassword", false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConnectionException))]
        public async Task SignInOnFakeUrl()
        {
            using (var conn = new Net.Fex.Api.Connection(new Uri("https://fake.net")))
            {
                var user = await conn.SignInAsync("fakelogin", "fakepassword", false);
            }
        }
    }
}
