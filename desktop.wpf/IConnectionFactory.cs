using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Fex.Api;

namespace FexSync
{
    public interface IConnectionFactory
    {
        IConnection CreateConnection();
    }
}
