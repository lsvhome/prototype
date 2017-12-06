using System;
using System.Collections.Generic;
using System.Text;

using AppKit;
using Foundation;

namespace FexSync.Mac
{
    public class PlatformServicesMac
    {
        public void AddTrayIcon()
        {
            // Construct menu that will be displayed when tray icon is clicked
            var notifyMenu = new NSMenu();
            var exitMenuItem = new NSMenuItem("Quit My Application", (a, b) => { System.Environment.Exit(0); }); // Just add 'Quit' command
            notifyMenu.AddItem(exitMenuItem);

            // Display tray icon in upper-right-hand corner of the screen
            var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
            statusItem.Menu = notifyMenu;
            statusItem.Image = NSImage.FromStream(System.IO.File.OpenRead(NSBundle.MainBundle.ResourcePath + @"/AppIcon-16.png"));
            statusItem.HighlightMode = true;

            // Remove the system tray icon from upper-right hand corner of the screen
            // (works without adjusting the LSUIElement setting in Info.plist)
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Accessory;
        }
    }
}
