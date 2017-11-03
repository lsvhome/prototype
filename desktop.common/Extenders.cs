using System;
using System.Collections.Generic;
using System.Text;

namespace desktop.common
{
    public static class Extenders
    {
        public static void Process(this Exception exception)
        {
            System.Diagnostics.Debugger.Break();
        }
    }
}
