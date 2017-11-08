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

        public CommandSignIn.User UserSignedIn { get; protected set; }

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

        public async Task<CommandSignIn.User> SignInAsync(string login, string password, bool stay_signed)
        {
            return await Task.Run<CommandSignIn.User>(() => { return this.SignIn(login, password, stay_signed); });
        }

        public CommandSignIn.User SignIn(string login, string password, bool stay_signed)
        {
            using (var cmd = new CommandSignIn(login, password, stay_signed))
            {
                cmd.Execute(this);
                this.UserSignedIn = cmd.Result.User;
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
            using (var cmd = new CommandLoginCheck(login))
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
            using (var cmd = new CommandSignUp(phone))
            {
                cmd.Execute(this);
            }
        }

        public async Task SignUpStep02Async(string code, string password, string login, string phone, string mail)
        {
            await Task.Run(() => { this.SignUpStep02(code, password, login, phone, mail); });
        }

        public void SignUpStep02(string code, string password, string login, string phone, string mail)
        {
            using (var cmd = new CommandSignUp(code, password, login, phone, mail))
            {
                cmd.Execute(this);
            }
        }

        public async Task<CommandArchive.CommandArchiveResponse> ArchiveAsync(int offset, int limit)
        {
            return await Task.Run<CommandArchive.CommandArchiveResponse>(() => { return this.Archive(offset, limit); });
        }

        public CommandArchive.CommandArchiveResponse Archive(int offset, int limit)
        {
            using (var cmd = new CommandArchive(offset, limit))
            {
                cmd.Execute(this);
                return cmd.Result;
            }
        }

        #endregion IConnection
    }
}