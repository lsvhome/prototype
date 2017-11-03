using System;
using System.Collections.Generic;
using System.Text;

namespace desktop.common
{
    public interface IPlatformServices: IDisposable
    {
        void AddTrayIcon();
    }
}
