﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FexSync
{
    public static class Extenders
    {
        internal static void Process(this Exception exception)
        {
#if DEBUG
            System.Diagnostics.Trace.WriteLine(exception.ToString());
            System.Diagnostics.Debug.Fail(exception.ToString());
            System.Diagnostics.Debugger.Break();
#endif
            System.Diagnostics.Debug.Fail(exception.ToString());
            System.Diagnostics.Debug.WriteLine(exception.ToString());


            //AppKit.
        }
    }
}
