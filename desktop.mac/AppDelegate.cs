using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using AppKit;
using Autofac;
using Desktop.Common;
using Foundation;

namespace desktop.mac
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate, IDisposable
    {
        public readonly Autofac.IContainer Container;

        public AppDelegate()
        {
            Dictionary<Type, object> mappings = new Dictionary<Type, object>
            {
                { typeof(Desktop.Common.IPlatformServices), new PlatformServicesMac() }
            };


            var builder = new ContainerBuilder();
            builder.RegisterInstance<Desktop.Common.IPlatformServices>(new PlatformServicesMac());
            builder.RegisterInstance<Net.Fex.Api.IConnection>(new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), new Uri("https://fex.net"), string.Format("FEX Sync ({0})", Net.Fex.Api.Connection.GetOSName())));
            //// builder.RegisterInstance<net.fex.api.v1.IConnection>(new net.fex.api.v1.BaseConnection());

            this.Container = builder.Build();
        }

        public void SignInSignOuTestOk()
        {
            const string loginValid = "slutai";
            const string passwordValid = "100~`!@#$%^&*()[]{}:;\"',<.>/?+=-_";
            using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), new Uri("https://fex.net"), string.Format("FEX Sync ({0})", Net.Fex.Api.Connection.GetOSName())))
            {
                var user = conn.SignIn(loginValid, passwordValid, false);
                System.Threading.Thread.Sleep(10000);
                conn.SignOut();
            }
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application

            // Construct menu that will be displayed when tray icon is clicked
            var notifyMenu = new NSMenu();
            var exitMenuItem = new NSMenuItem("Quit My Application",
                (a, b) => { System.Environment.Exit(0); }); // Just add 'Quit' command
            notifyMenu.AddItem(exitMenuItem);

            var loginLogoutMenuItem = new NSMenuItem("LoginLogout",
                (a, b) => {
                    try
                    {
                        SignInSignOuTestOk();
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debugger.Break();
                        throw;
                    }
                    //System.Environment.Exit(0);
                });
            notifyMenu.AddItem(loginLogoutMenuItem);

            // Display tray icon in upper-right-hand corner of the screen
            var sItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
            sItem.Menu = notifyMenu;
            sItem.Image = NSImage.FromStream(System.IO.File.OpenRead(
                NSBundle.MainBundle.ResourcePath + @"/AppIcon-16.png"));
            sItem.HighlightMode = true;

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
