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
using Net.Fex.Api;

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

        public TaskbarIcon NotifyIcon { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                //// create the notifyicon (it's a resource declared in NotifyIconResources.xaml
                this.NotifyIcon = (TaskbarIcon)App.Current.FindResource("NotifyIcon");
                this.NotifyIcon.DataContext = new NotifyIconViewModel();


                if (CredentialsManager.Exists())
                {
                    this.SyncWorkflow.Start();
                }
                else
                {
                    //// First Application Run
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

                            this.SyncWorkflow.Start();
                            //if (Application.Current.MainWindow.ShowDialog() == true)
                            //{
                                
                            //}
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

        private void StartWhenCredentialsValid()
        {
            //this.syncWorkflowException = null;
            //this.SyncWorkflow.OnException += SyncWorkflow_OnExceptionAtStart;
            //this.SyncWorkflow.Start();

//            Task.Run(() =>
//            {
//#if DEBUG
//                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
//#else
//                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(60));
//#endif
//                this.SyncWorkflow.OnException -= SyncWorkflow_OnExceptionAtStart;
//                if (this.syncWorkflowException != null)
//                {
//                    Application.Current.Dispatcher.Invoke(() =>
//                    {
//                        var ok = new AuthWindow().ShowDialog();
//                        if ( ok == true)
//                        {
//                            this.StartWhenCredentialsValid();
//                        }
//                    });
//                }
//            });
        }

        //private Exception syncWorkflowException = null;

        //private void SyncWorkflow_OnExceptionAtStart(object sender, Net.Fex.Api.Connection.ExceptionEventArgs e)
        //{
        //    if (e.Exception is CaptchaRequiredException)
        //    {
        //        syncWorkflowException = e.Exception;
        //    }

        //    if (e.Exception is ConnectionException)
        //    {
        //        syncWorkflowException = e.Exception;
        //    }
        //}

        protected override void OnExit(ExitEventArgs e)
        {
            if (this.SyncWorkflow.Status == FexSync.SyncWorkflow.SyncWorkflowStatus.Started)
            {
                this.SyncWorkflow.Stop();
            }

            this.Container.Dispose();
            this.NotifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}
