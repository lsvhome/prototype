using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Fex.Api
{
    public interface IConnectionCommand : IDisposable
    {
        bool CanExecute(IConnection connection);

        void Execute(IConnection connection);
    }
}
