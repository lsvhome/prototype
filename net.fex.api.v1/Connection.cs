using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class Connection : IConnection
    {
        #region

        public static string GetOSName()
        {
#if NETSTANDARD1_6
            OSPlatform platform = OSPlatform.Create("Other Platform");
            //// Check if it's windows 
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            platform = isWindows ? OSPlatform.Windows : platform;
            //// Check if it's osx 
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            platform = isOSX ? OSPlatform.OSX : platform;
            //// Check if it's Linux 
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            platform = isLinux ? OSPlatform.Linux : platform;
            return platform.ToString();
#elif NET461
            return Environment.OSVersion.VersionString;
#else
            throw new NotImplementedException();
#endif
        }

        public System.Net.Http.HttpClient Client { get; private set; } = new System.Net.Http.HttpClient();

        public Uri Endpoint { get; private set; }

        public bool IsSignedIn
        {
            get
            {
                return this.UserSignedIn != null;
            }
        }

        public User UserSignedIn { get; protected set; }

        public Connection(Uri endpoint)
        {
            this.Endpoint = endpoint;
            this.Client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", string.Format("FEX Desktop ({0})", GetOSName()));
        }

        public void Dispose()
        {
            this.Client.Dispose();
        }

        #endregion

        #region IConnection

        public async Task<User> SignInAsync(string login, string password, bool stay_signed)
        {
            return await Task.Run<User>(() => { return this.SignIn(login, password, stay_signed); });
        }

        public User SignIn(string login, string password, bool stay_signed)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "login", login },
                { "password", password },
                { "stay_signed", stay_signed ? "1" : "0" }
            };

            using (var cmd = new CommandSignIn(parameters))
            {
                cmd.Execute(this);
                this.UserSignedIn = cmd.Result;
                return this.UserSignedIn;
            }
        }

        public async Task SignOutAsync()
        {
            await Task.Run(() => { this.SignOut(); });
        }

        public void SignOut()
        {
            using (var cmd = new CommandSignOut())
            {
                cmd.Execute(this);
                this.UserSignedIn = null;
            }
        }

        public async Task<bool> LoginCheckAsync(string login)
        {
            return await Task.Run<bool>(() => { return this.LoginCheck(login); });
        }

        public bool LoginCheck(string login)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "login", login }
            };
            using (var cmd = new CommandLoginCheck(parameters))
            {
                cmd.Execute(this);
                return cmd.Result;
            }
        }

        public async Task SignUpStep01Async(string phone)
        {
            await Task.Run(() => { this.SignUpStep01(phone); });
        }

        public void SignUpStep01(string phone)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "phone", phone }
            };

            using (var cmd = new CommandSignUp(parameters))
            {
                cmd.Execute(this);
            }
        }

        public async Task SignUpStep02Async(string phone, string captcha_token, string captcha_value)
        {
            await Task.Run(() => { this.SignUpStep02(phone, captcha_token, captcha_value); });
        }

        public void SignUpStep02(string phone, string captcha_token, string captcha_value)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "phone", phone },
                { "captcha_token", captcha_token },
                { "captcha_value", captcha_value }
            };

            using (var cmd = new CommandSignUp(parameters))
            {
                cmd.Execute(this);
            }
        }

        #endregion IConnection
    }
}