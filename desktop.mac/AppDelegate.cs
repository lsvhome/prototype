using System;
using System.Collections;
using System.Collections.Generic;

using AppKit;
using Foundation;

namespace desktop.mac
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public static desktop.common.IIocWrapper container = new desktop.common.IocWrapper();

        public AppDelegate()
        {
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
            Dictionary<Type, object> mappings = new Dictionary<Type, object>();
            mappings.Add(typeof(desktop.common.IPlatformServices), new PlatformServicesMac());
            container.Init(mappings);
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}
