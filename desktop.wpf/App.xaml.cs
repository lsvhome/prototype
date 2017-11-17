﻿using System;
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
                    if (exceptionObjectParam.ExceptionObject is Exception exception)
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
                else
                {
                    var quickStart = new QuickStartWindow();
                    quickStart.Closed += (object sender, EventArgs args) =>
                    {
                        var auth = new AuthWindow();
                        if (auth.ShowDialog() == true)
                        {
                            if (Application.Current.MainWindow == null)
                            {
                                Application.Current.MainWindow = new SettingsWindow();
                            }

                            Application.Current.MainWindow.Show();
                        }
                    };
                    quickStart.Show();

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
