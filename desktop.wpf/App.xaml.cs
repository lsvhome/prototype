using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

using Autofac;
using Desktop.Common;
using Hardcodet.Wpf.TaskbarNotification;

namespace FexSync
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
                AppDomain.CurrentDomain.UnhandledException += (source, exceptionObjectParam) =>
                {
                    Exception exception = exceptionObjectParam.ExceptionObject as Exception;
                    if (exception != null)
                    {
                        System.Diagnostics.Trace.Fail(exception.ToString());
                    }
                    else
                    {
                        System.Diagnostics.Trace.Fail("Exception is empty");
                    }
                };

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

                if (CredentialsManager.Exists())
                {
                    this.SyncWorkflow.Start();
                }
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (this.SyncWorkflow.Status == FexSync.SyncWorkflow.SyncWorkflowStatus.Started)
            {
                this.SyncWorkflow.Stop();
            }

            this.Container.Dispose();
            base.OnExit(e);
        }
    }
}
