using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using desktop.common;
using Autofac;

namespace desktop.wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public readonly IIocWrapper Container = new desktop.common.IocWrapper();

        public App()
        {
            try
            {
                InitContainer();
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }

        public void InitContainer()
        {
            //Dictionary<Type, object> mappings = new Dictionary<Type, object>();
            //mappings.Add(typeof(desktop.common.IPlatformServices), new PlatformServicesWPF());
            //mappings.Add(typeof(net.fex.api.v1.IConnection), new net.fex.api.v1.Connection(new Uri("https://fex.net")));
            //this.Container.Init(mappings);

            var builder = new ContainerBuilder();
            builder.RegisterInstance<desktop.common.IPlatformServices>(new PlatformServicesWPF());
            builder.RegisterInstance<net.fex.api.v1.IConnection>(new net.fex.api.v1.Connection(new Uri("https://fex.net")));

            ((IocWrapper)this.Container).container = builder.Build();

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
                this.Container.Get<IPlatformServices>().AddTrayIcon();
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this.Container.Dispose();
            base.OnExit(e);
        }
    }
}
