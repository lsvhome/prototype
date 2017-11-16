using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Fex.Api;

namespace FexSync
{
    public class ConnectionFactory : IConnectionFactory
    {
        public IConnection CreateConnection()
        {
            return new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), new Uri(System.Configuration.ConfigurationManager.AppSettings["FEX.NET.ApiHost"]), string.Format("FEX Sync ({0})", Net.Fex.Api.Connection.GetOSName()));
        }
    }
}
