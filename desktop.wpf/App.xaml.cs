using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Desktop.Common;
using Hardcodet.Wpf.TaskbarNotification;

namespace Desktop.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public readonly Autofac.IContainer Container;
        public readonly SyncWorkflow SyncWorkflow = new SyncWorkflow();

        public App()
        {
            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterInstance<Desktop.Common.IPlatformServices>(new PlatformServicesWPF());
                builder.RegisterInstance<IConnectionFactory>(new ConnectionFactory());

                this.Container = builder.Build();
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                //// create the notifyicon (it's a resource declared in NotifyIconResources.xaml
                this.Container.Resolve<IPlatformServices>().AddTrayIcon();
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
