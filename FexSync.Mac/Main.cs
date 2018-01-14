using System.Linq;
using AppKit;

namespace FexSync.Mac
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            System.Diagnostics.Logger.Enabled = args?.Contains("net.fex.api.trace") == true;

            NSApplication.Init();
            NSApplication.Main(args);
        }
    }
}
