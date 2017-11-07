using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public abstract class CommandBase : IConnectionCommand
    {
        public virtual void Dispose()
        {
        }

        public CommandBase() : this(new Dictionary<string, string>())
        {
        }

        public CommandBase(IDictionary<string, string> parameters)
        {
            this.Parameters = parameters;
        }

        protected readonly IDictionary<string, string> Parameters;

        protected abstract string Suffix { get; }

        protected virtual JObject ResultJObject { get; set; } = new JObject();

        public virtual bool CanExecute(IConnection connection)
        {
            return true;
        }

        protected Uri BuildUrl(IConnection connection)
        {
            var uriBuilder = new UriBuilder(connection.Endpoint);

            uriBuilder.Path += this.Suffix;

            if (this.Parameters != null && this.Parameters.Keys.Any())
            {
                uriBuilder.Query = string.Join("&", this.Parameters.Select(item => string.Format("{0}={1}", item.Key, System.Net.WebUtility.UrlEncode(item.Value))));
            }

            return uriBuilder.Uri;
        }

        public virtual void Execute(IConnection connection)
        {
            try
            {
                if (this.CanExecute(connection))
                {
                    var uri = this.BuildUrl(connection);
                    System.Diagnostics.Debug.WriteLine(uri);
                    using (var response = connection.Client.GetAsync(uri).Result)
                    {
                        string responseJson = response.Content.ReadAsStringAsync().Result;
                        System.Diagnostics.Debug.WriteLine(responseJson);
                        this.ResultJObject = Newtonsoft.Json.Linq.JObject.Parse(responseJson);
                    }
                }
                else
                {
                    throw new ConnectionException() { ErrorCode = 5000 };
                }
            }
            catch (ConnectionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ConnectionException(ex.Message, ex) { ErrorCode = 5008 };
            }
        }
    }
}
