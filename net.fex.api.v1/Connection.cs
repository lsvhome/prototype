using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace net.fex.api.v1
{
    public interface IConnection : IDisposable
    {
        Task<User> SignInAsync(string login, string password, bool stay_signed);
        Task SignOutAsync();
    }

    public class Connection : IConnection
    {
        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

        public class ConnectionException : Exception
        {
            public ConnectionException()
            {
            }

            public ConnectionException(string message) : base(message)
            {
            }

            public ConnectionException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public int ErrorCode { get; set; }

            public string HttpResponse { get; set; }
        }

        public class LoginException : ConnectionException
        {
            public LoginException(string message, int id) : base(message)
            {
                this.Id = id;
            }

            public int Id { get; set; }
        }

        private Uri endpoint;

        public Connection(Uri endpoint)
        {
            this.endpoint = endpoint;
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", string.Format("FEX Desktop ({0})", GetOSName()));
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        private Uri BuildUrl(string suffix, params KeyValuePair<string, string>[] queryParams)
        {
            var uriBuilder = new UriBuilder(this.endpoint);

            uriBuilder.Path += suffix;

            uriBuilder.Query = string.Join("&", queryParams.Select(item => string.Format(item.Key, System.Net.WebUtility.UrlEncode(item.Value))));

            return uriBuilder.Uri;
        }

        private Uri BuildUrl(string suffix)
        {
            var uriBuilder = new UriBuilder(this.endpoint);

            uriBuilder.Path += suffix;

            return uriBuilder.Uri;
        }

        public static string GetOSName()
        {
#if NETSTANDARD1_6
            OSPlatform osPlatform = OSPlatform.Create("Other Platform");
            // Check if it's windows 
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            osPlatform = isWindows ? OSPlatform.Windows : osPlatform;
            // Check if it's osx 
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            osPlatform = isOSX ? OSPlatform.OSX : osPlatform;
            // Check if it's Linux 
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            osPlatform = isLinux ? OSPlatform.Linux : osPlatform;
            return osPlatform.ToString();
#elif NET461
            return Environment.OSVersion.VersionString;
#else
            throw new NotImplementedException();
#endif
        }

        public async Task<User> SignInAsync(string login, string password, bool stay_signed)
        {
            var uri = this.BuildUrl("j_signin");
            uri = uri
                .AppendQuery("login", login)
                .AppendQuery("password", password)
                .AppendQuery("stay_signed", stay_signed ? "1" : "0");

            using (var response = await client.GetAsync(uri))
            {
                string responseJson = string.Empty;
                try
                {
                    responseJson = await response.Content.ReadAsStringAsync();
                    JObject responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseJson);
                    if (responseObject.Value<int>("result") == 1)
                    {
                        JObject jUser = responseObject.Value<JObject>("user");
                        return new User(jUser.Value<string>("login"), jUser.Value<int>("priv"));
                    }
                    else
                    {
                        JObject jErr = responseObject.Value<JObject>("err");
                        string message = jErr.Value<string>("msg");
                        int id = jErr.Value<int>("id");
                        string captcha = responseObject.Value<string>("captcha");
                        var ex = new LoginException(message, id) { ErrorCode = 1001, HttpResponse = responseJson };
                        throw ex;
                    }
                }
                catch (LoginException)
                {
                    throw;
                }
                catch
                {
                    throw new ConnectionException() { ErrorCode = 1002, HttpResponse = responseJson };
                }
            }
        }

        public async Task SignOutAsync()
        {
            var uri = this.BuildUrl("j_signout");

            using (var response = await client.GetAsync(uri))
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                JObject responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseJson);
                if (responseObject.Value<int>("result") == 1)
                {
                    return;
                }
                else
                {
                    throw new ConnectionException() { ErrorCode = 1003, HttpResponse = responseJson };
                }
            }
        }

        public async Task IndexAsync()
        {
            try
            {
                var uri = this.BuildUrl("/j_index");

                using (var response = await client.GetAsync(uri))
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    JObject responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseJson);
                    if (responseObject.Value<int>("result") == 1)
                    {
                        return;
                    }
                    else
                    {
                        throw new ConnectionException() { ErrorCode = 1004, HttpResponse = responseJson };
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }
    }
}