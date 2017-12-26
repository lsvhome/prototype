using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Fex.Api;

namespace FexSync.Data
{
    public class ConnectionFactory : IConnectionFactory
    {
        public IConnection CreateConnection(Uri endPoint)
        {
            return this.CreateConnection(endPoint, new System.Threading.CancellationTokenSource().Token);
        }

        public IConnection CreateConnection(Uri endPoint, System.Threading.CancellationToken cancellationToken)
        {
            var d = new System.IO.FileSystemWatcher();
            var httpClientWrapper = new Net.Fex.Api.HttpClientWrapper();
            var userAgent = string.Format("FEX Sync ({0})", Net.Fex.Api.Connection.GetOSName());
            return new Net.Fex.Api.Connection(httpClientWrapper, endPoint, userAgent, cancellationToken);
        }
    }
}
