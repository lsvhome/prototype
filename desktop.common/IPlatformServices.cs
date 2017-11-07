using System;
using System.Collections.Generic;
using System.Text;

namespace Desktop.Common
{
    public interface IPlatformServices : IDisposable
    {
        void AddTrayIcon();
    }
}
