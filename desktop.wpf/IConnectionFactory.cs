using Net.Fex.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.Wpf
{
    public interface IConnectionFactory
    {
        IConnection CreateConnection();
    }
}
