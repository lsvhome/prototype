using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using AppKit;
using Autofac;
using Foundation;

namespace FexSync.Mac
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate, IDisposable
    {
        public readonly Autofac.IContainer Container;

        public AppDelegate()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<Net.Fex.Api.IConnection>(new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), new Uri("https://fex.net"), string.Format("FEX Sync ({0})", Net.Fex.Api.Connection.GetOSName())));
            this.Container = builder.Build();
        }

#if DEBUG
        public void SignInSignOuTestOk()
        {


            var t = new FexSync.Data.SyncDataDbContext("~/aaa.db");
            /*
            string loginValid = "username";
            string passwordValid = "password";
            var conn = this.Container.Resolve<Net.Fex.Api.IConnection>();
            //// using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), new Uri("https://fex.net"), string.Format("FEX Sync ({0})", Net.Fex.Api.Connection.GetOSName())))
            {
                var user = conn.SignIn(loginValid, passwordValid, false);
                System.Threading.Thread.Sleep(10000);
                conn.SignOut();
            }
            */
        }
#endif

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application

            // Construct menu that will be displayed when tray icon is clicked
            var notifyMenu = new NSMenu();
            var exitMenuItem = new NSMenuItem(
                "Quit My Application",
                (a, b) => { System.Environment.Exit(0); }); // Just add 'Quit' command
            notifyMenu.AddItem(exitMenuItem);

            var loginLogoutMenuItem = new NSMenuItem(
                "LoginLogout",
                (a, b) => 
                {
                    try
                    {
                        SignInSignOuTestOk();
                    }
                    catch (Exception ex)
                    {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                        System.Diagnostics.Debugger.Break();
                        throw;
                    }
                });

            notifyMenu.AddItem(loginLogoutMenuItem);

            // Display tray icon in upper-right-hand corner of the screen
            var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
            statusItem.Menu = notifyMenu;
            statusItem.Image = NSImage.FromStream(System.IO.File.OpenRead(
                NSBundle.MainBundle.ResourcePath + @"/AppIcon-16.png"));
            statusItem.HighlightMode = true;

            // Remove the system tray icon from upper-right hand corner of the screen
            // (works without adjusting the LSUIElement setting in Info.plist)
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Accessory;
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
            this.Container.Dispose();
        }
    }
}
