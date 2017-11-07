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
    public class Connection : BaseConnection
    {
        #region

        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

        private Uri endpoint;

        public Connection(Uri endpoint)
        {
            this.endpoint = endpoint;
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", string.Format("FEX Desktop ({0})", GetOSName()));
        }

        public override void Dispose()
        {
            this.client.Dispose();
            base.Dispose();
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

        #endregion

        #region IConnection

        public override User SignIn(string login, string password, bool stay_signed)
        {
            //return this.SignInAsync(login, password, stay_signed).Result;

            var uri = this.BuildUrl("j_signin");
            uri = uri
                .AppendQuery("login", login)
                .AppendQuery("password", password)
                .AppendQuery("stay_signed", stay_signed ? "1" : "0");

            using (var response = client.GetAsync(uri).Result)
            {
                string responseJson = string.Empty;
                try
                {
                    responseJson = response.Content.ReadAsStringAsync().Result;
                    JObject responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseJson);
                    if (responseObject.Value<int>("result") == 1)
                    {
                        JObject jUser = responseObject.Value<JObject>("user");
                        this.UserSignedIn = new User(jUser.Value<string>("login"), jUser.Value<int>("priv"));
                        return this.UserSignedIn;
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

        public override void SignOut()
        {
            this.UserSignedIn = null;
            var uri = this.BuildUrl("j_signout");

            using (var response = client.GetAsync(uri).Result)
            {
                string responseJson = response.Content.ReadAsStringAsync().Result;
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
        
        #endregion IConnection
    }
}