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

        private readonly Uri uriValid = new Uri("https://fex.net");

        [TestMethod]
        public async Task SignInSignOuTestOk()
        {
            using (var conn = new Net.Fex.Api.Connection(this.uriValid))
            {
                Assert.IsFalse(conn.LoginCheck(LoginValid));

                Assert.IsFalse(conn.IsSignedIn);

                var user = await conn.SignInAsync(LoginValid, PasswordValid, false);
                Assert.IsNotNull(user);
                Assert.AreEqual(LoginValid, user.Login);

                Assert.IsTrue(conn.IsSignedIn);

                await conn.SignOutAsync();

                Assert.IsFalse(conn.IsSignedIn);
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

            using (var conn = new Net.Fex.Api.Connection(this.uriValid))
            {
                await conn.SignUpStep01Async(phone);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApiErrorException))]
        public async Task SignInFailFakeLogin()
        {
            try
            {
                using (var conn = new Net.Fex.Api.Connection(this.uriValid))
                {
                    await conn.SignInAsync("fakelogin", "fakepassword", false);
                }
            }
            catch (ApiErrorException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ApiErrorException))]
        public async Task SignInFailFakePassword()
        {
            using (var conn = new Net.Fex.Api.Connection(this.uriValid))
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

        [TestMethod]
        public async Task GetArchive()
        {
            using (var conn = new Net.Fex.Api.Connection(this.uriValid))
            {
                Assert.IsFalse(conn.LoginCheck(LoginValid));

                Assert.IsFalse(conn.IsSignedIn);

                var user = await conn.SignInAsync(LoginValid, PasswordValid, false);
                Assert.IsNotNull(user);
                Assert.AreEqual(LoginValid, user.Login);

                Assert.IsTrue(conn.IsSignedIn);

                var files = await conn.ArchiveAsync(0, 1);

                Assert.IsNotNull(files);
                Assert.AreEqual(0, files.Offset);
                Assert.AreEqual(1, files.Limit);
                Assert.AreEqual(1, files.Result);
                Assert.AreEqual(1, files.Count);

                Assert.IsNotNull(files.ObjectList as CommandArchive.CommandArchiveResponseObject[]);
                Assert.AreEqual(1, files.ObjectList.Length);

                var firstFile = files.ObjectList.First();
                Assert.AreEqual(LoginValid, firstFile.Login);
                Assert.IsNotNull(firstFile.Token);
                Assert.IsNotNull(firstFile.Preview);
                Assert.AreEqual(1, firstFile.UploadCount);
                Assert.AreEqual(1, firstFile.Pay);
                Assert.AreEqual(0, firstFile.WithViewPass);

                await conn.SignOutAsync();

                Assert.IsFalse(conn.IsSignedIn);
            }
        }
    }
}
