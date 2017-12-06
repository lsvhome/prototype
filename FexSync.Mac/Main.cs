using AppKit;

namespace FexSync.Mac
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            NSApplication.Init();
            NSApplication.Main(args);
        }
    }
}
