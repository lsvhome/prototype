using System;
using System.Collections.Generic;
using System.Text;

namespace Desktop.Common
{
    public static class Extenders
    {
        public static void Process(this Exception exception)
        {
            System.Diagnostics.Debugger.Break();
        }
    }
}
