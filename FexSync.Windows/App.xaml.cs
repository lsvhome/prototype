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
            System.Diagnostics.Logger.Enabled = System.Configuration.ConfigurationManager.AppSettings["net.fex.api.trace"] == "true";

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (source, exceptionObjectParam) =>
                {
                    if (exceptionObjectParam.ExceptionObject is Exception exception)
                    {
                        exception.Process();
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

                System.Diagnostics.Trace.WriteLine("Startup Trace");
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
            builder.RegisterInstance<FexSync.Data.ISyncDataDbContext>(new FexSync.Data.SyncDataDbContext(ApplicationSettingsManager.AccountCacheDbFile));
            builder.RegisterInstance<FexSync.Data.IFileSystemWatcher>(new FexSync.WindowsFileSystemWatcher());

            if (this.Container != null)
            {
                this.Container.Dispose();
                this.Container = null;
            }

            this.Container = builder.Build();
        }

        public void ConfigureSyncWorkflow()
        {
            var config = new SyncWorkflow.SyncWorkflowConfig();
            config.ApiHost = ApplicationSettingsManager.ApiHost;
            config.Container = this.Container;
            config.Account = config.Container.Resolve<ISyncDataDbContext>().Accounts.Single();
            config.SyncObjects = config.Container.Resolve<ISyncDataDbContext>().AccountSyncObjects.ToArray();

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
                var iconDataContext = new NotifyIconViewModel();
                this.NotifyIcon.DataContext = iconDataContext;
                SyncWorkflow.Singleton.Instance.OnStatusChanged += (sender, args) => { iconDataContext.FireSyncStatusChanged(); };

                bool accountExists = false;
                using (var db0 = new FexSync.Data.SyncDataDbContext(ApplicationSettingsManager.AccountCacheDbFile))
                {
                    db0.EnsureDatabaseExists();
                    accountExists = db0.Accounts.Any();
                }

                if (accountExists)
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
                        //// build temporary container
                        var builder = new ContainerBuilder();
                        builder.RegisterInstance<IConnectionFactory>(new Data.ConnectionFactory());
                        builder.RegisterInstance<FexSync.Data.ISyncDataDbContext>(new FexSync.Data.SyncDataDbContext(ApplicationSettingsManager.AccountCacheDbFile));
                        this.Container = builder.Build();

                        using (var conn = ((App)App.Current).Container.Resolve<Data.IConnectionFactory>().CreateConnection(new Uri(ApplicationSettingsManager.ApiHost)))
                        {
                            Account account = null;
                            var authWindow = new AuthWindow(conn);
                            authWindow.OnSignedIn += (object sender1, CommandSignIn.SignInEventArgs signedUserArgs) =>
                            {
                                var syncDb = this.Container.Resolve<ISyncDataDbContext>();
                                syncDb.LockedRun(() =>
                                {
                                    account = syncDb.Accounts.SingleOrDefault();
                                    if (account == null)
                                    {
                                        account = new Account();
                                        syncDb.Accounts.Add(account);
                                    }

                                    account.Login = signedUserArgs.Login;
                                    account.Password = signedUserArgs.Password;

                                    syncDb.SaveChanges();

                                    account.EnsureAccountHasDefaultSyncObject(syncDb, conn);
                                });

                                this.Dispatcher.Invoke(() =>
                                {
                                    SettingsWindow settings = new SettingsWindow(account, conn);
                                    settings.ShowDialog();
                                });

                                this.ConfigureContainer();
                                this.ConfigureSyncWorkflow();

                                SyncWorkflow.Singleton.Instance.Start();
                            };

                            authWindow.ShowDialog();
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
            System.Diagnostics.Trace.WriteLine("Exit Trace");
            System.Diagnostics.Trace.WriteLine("Exit Debug");
        }
    }
}
