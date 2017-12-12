using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

using Autofac;
using FexSync.Data;
using Hardcodet.Wpf.TaskbarNotification;
using Net.Fex.Api;

[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension = "config", Watch = true)]

namespace FexSync
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string NotifyIconKey = "NotifyIcon";

        public Autofac.IContainer Container { get; private set; }

        public SyncWorkflow SyncWorkflow
        {
            get
            {
                return SyncWorkflow.Singleton.Instance;
            }
        }

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

                if (!Directory.Exists(ApplicationSettingsManager.ApplicationDataFolder))
                {
                    Directory.CreateDirectory(ApplicationSettingsManager.ApplicationDataFolder);
                }

                //// Required for log4net setup
                log4net.ILog log = log4net.LogManager.GetLogger(typeof(App));

                System.Diagnostics.Debug.WriteLine("Startup Trace");
                System.Diagnostics.Trace.WriteLine("Startup Debug");
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }

        public TaskbarIcon NotifyIcon { get; set; }

        public void ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConnectionFactory>(new Data.ConnectionFactory());
            var fn = ApplicationSettingsManager.AccountSettings.AccountCacheDbFile;
            builder.RegisterInstance<FexSync.Data.ISyncDataDbContext>(new FexSync.Data.SyncDataDbContext(fn));

            if (this.Container != null)
            {
                this.Container.Dispose();
            }

            this.Container = builder.Build();
        }

        public void ConfigureSyncWorkflow()
        {
            var config = new SyncWorkflow.SyncWorkflowConfig();

            config.AccountSettings = ApplicationSettingsManager.AccountSettings;
            config.ApiHost = ApplicationSettingsManager.ApiHost;
            config.Container = this.Container;
            this.SyncWorkflow.Reconfigure(config);
        }

        private void ValidateConfig()
        {
            System.Diagnostics.Debug.Assert(Directory.Exists(ApplicationSettingsManager.ApplicationDataFolder), "ApplicationDataFolder does not exists. Check Setings.");
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(ApplicationSettingsManager.ApiHost), "FEX.NET.ApiHost is undefined. Check Setings.");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

#if DEBUG
                this.ValidateConfig();
#endif

                //// create the notifyicon (it's a resource declared in NotifyIconResources.xaml
                this.NotifyIcon = (TaskbarIcon)((App)App.Current).FindResource("NotifyIcon");

                this.NotifyIcon.DataContext = new NotifyIconViewModel();

                if (ApplicationSettingsManager.AccountSettings.Exists())
                {
                    this.ConfigureContainer();
                    this.ConfigureSyncWorkflow();
                    this.SyncWorkflow.Start();
                }
                else
                {
                    //// First Application Run
                    var quickStart = new QuickStartWindow();
                    quickStart.Closed += (object sender, EventArgs args) =>
                    {
                        using (var conn = ((App)App.Current).Container.Resolve<Data.IConnectionFactory>().CreateConnection(new Uri(ApplicationSettingsManager.ApiHost)))
                        {
                            var authWindow = new AuthWindow(conn);
                            authWindow.OnSignedIn += (object sender1, CommandSignIn.SignInEventArgs signedUserArgs) =>
                            {
                                using (var cmd = new CommandEnsureDefaultObjectExists(ApplicationSettingsManager.DefaultFexSyncFolderName))
                                {
                                    cmd.Execute(signedUserArgs.Connection);

                                    ApplicationSettingsManager.AccountSettings.Login = signedUserArgs.Login;
                                    ApplicationSettingsManager.AccountSettings.Password = signedUserArgs.Password;
                                    ApplicationSettingsManager.AccountSettings.TokenForSync = cmd.Result;
                                }
                            };

                            if (authWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(ApplicationSettingsManager.AccountSettings.Login))
                            {
                                SettingsWindow settings = new SettingsWindow();

                                settings.UserDataFolder = ApplicationSettingsManager.CurrentFexUserRootFolder;
                                if (settings.ShowDialog() == true)
                                {
                                    ApplicationSettingsManager.CurrentFexUserRootFolder = settings.TxtCurrentDataFolder.Text;
                                }

                                this.ConfigureContainer();

                                this.ConfigureSyncWorkflow();

                                SyncWorkflow.Singleton.Instance.Start();
                            }
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

        private void Auth_OnSignedIn(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Hide tray icon first: user will not run any command after that;
            this.NotifyIcon.Dispose();

            if (this.SyncWorkflow != null)
            {
                this.SyncWorkflow.Dispose();
            }

            if (this.Container != null)
            {
                this.Container.Dispose();
            }

            base.OnExit(e);
            System.Diagnostics.Debug.WriteLine("Exit Trace");
            System.Diagnostics.Trace.WriteLine("Exit Debug");
        }
    }
}
