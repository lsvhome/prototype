﻿using Net.Fex.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.Wpf
{
    public class ConnectionFactory : IConnectionFactory
    {
        public IConnection CreateConnection()
        {
            return new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), new Uri("https://fex.net"), string.Format("FEX Sync ({0})", Net.Fex.Api.Connection.GetOSName()));
        }
    }
}
