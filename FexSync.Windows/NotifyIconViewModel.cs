using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Autofac;
using FexSync.Data;
using Net.Fex.Api;

namespace FexSync
{
    /// <summary>
    /// Provides bindable properties and commands for the NotifyIcon. In this sample, the
    /// view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
    /// in App.xaml.cs could have created this view model, and assigned it to the NotifyIcon.
    /// </summary>
    public class NotifyIconViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool AccountSettingsExists
        {
            get
            {
                try
                {
                    var db = ((App)Application.Current).Container.Resolve<ISyncDataDbContext>();
                    bool ret = false;
                    db.LockedRun(() =>
                    {
                        ret = db.Accounts.Any();
                    });

                    return ret;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private SyncWorkflow SyncWorkflow
        {
            get
            {
                if (!this.AccountSettingsExists)
                {
                    return null;
                }

                return ((App)App.Current).SyncWorkflow;
            }
        }

        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand ShowSettingsCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => this.AccountSettingsExists && (SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Started || SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped),
                    CommandAction = () =>
                    {
                        try
                        {
                            SettingsWindow settings = new SettingsWindow();
                            Application.Current.MainWindow = settings;

                            var syncDb = ((App)App.Current).Container.Resolve<ISyncDataDbContext>();

                            var defaultSyncObject = syncDb.AccountSyncObjects.Single();
                            settings.UserDataFolder = defaultSyncObject.Path;

                            if (settings.ShowDialog() == true)
                            {
                                var newFolder = settings.UserDataFolder;

                                //// required full restart
                                Task.Run(() =>
                                {
                                    var app = (App)Application.Current;
                                    if (app.SyncWorkflow != null)
                                    {
                                        bool doStopRecofigureStart = app.SyncWorkflow.Status != SyncWorkflow.SyncWorkflowStatus.Stopped;

                                        EventHandler onStoppedSyncWorkflowEventHandler = null;

                                        onStoppedSyncWorkflowEventHandler = (sender, args) =>
                                        {
                                            Task.Run(() =>
                                            {
                                                syncDb.LockedRun(() =>
                                                { 
                                                    defaultSyncObject.Path = newFolder;
                                                    syncDb.SaveChanges();
                                                });

                                                app.ConfigureContainer();

                                                app.ConfigureSyncWorkflow();

                                                if (doStopRecofigureStart)
                                                {
                                                    app.SyncWorkflow.OnStopped -= onStoppedSyncWorkflowEventHandler;

                                                    System.Threading.Thread.Sleep(1);
                                                    app.SyncWorkflow.Start();
                                                }
                                            });
                                        };

                                        if (doStopRecofigureStart)
                                        {
                                            app.SyncWorkflow.OnStopped += onStoppedSyncWorkflowEventHandler;
                                            app.SyncWorkflow.Stop();
                                        }
                                        else
                                        {
                                            onStoppedSyncWorkflowEventHandler(this, new EventArgs());
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.Process();
                        }
                    }
                };
            }
        }
        
        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand QuickStartCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => true,
                    CommandAction = () =>
                    {
                        QuickStartWindow quickStartWindow = new QuickStartWindow();
                        quickStartWindow.Show();
                    }
                };
            }
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
            }
        }

        public ICommand ConnectCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => this.AccountSettingsExists && SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped,
                    CommandAction = () =>
                    {
                        try
                        {
                            ((App)App.Current).SyncWorkflow.Start();
                        }
                        catch (Exception ex)
                        {
                            ex.Process();
                        }
                    }
                };
            }
        }

        public ICommand DisconnectCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => this.AccountSettingsExists && (SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Started || SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Starting),

                    CommandAction = () =>
                    {
                        try
                        {
                            this.SyncWorkflow.Stop();
                        }
                        catch (ApiErrorException ex)
                        {
                            ex.Process();
                        }
                        catch (ConnectionException ex)
                        {
                            ex.Process();
                        }
                        catch (Exception ex)
                        {
                            ex.Process();
                        }
                    }
                };
            }
        }

        public ICommand SignInCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => !this.AccountSettingsExists && SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped,

                    CommandAction = () =>
                    {
                        try
                        {
                            using (var conn = ((App)App.Current).Container.Resolve<Data.IConnectionFactory>().CreateConnection(new Uri(ApplicationSettingsManager.ApiHost)))
                            {
                                var authWindow = new AuthWindow(conn);
                                authWindow.OnSignedIn += (object sender1, CommandSignIn.SignInEventArgs signedUserArgs) =>
                                {
                                    using (var cmd = new CommandEnsureDefaultObjectExists(ApplicationSettingsManager.DefaultFexSyncFolderName))
                                    {
                                        cmd.Execute(signedUserArgs.Connection);

                                        var db = ((App)Application.Current).Container.Resolve<ISyncDataDbContext>();
                                        db.LockedRun(() =>
                                        {
                                            var account = db.Accounts.SingleOrDefault();
                                            if (account == null)
                                            {
                                                account = new Account();
                                                db.Accounts.Add(account);
                                            }

                                            account.Login = signedUserArgs.Login;
                                            account.Password = signedUserArgs.Password;

                                            var syncObject = db.AccountSyncObjects.SingleOrDefault();
                                            if (syncObject == null)
                                            {
                                                syncObject = new AccountSyncObject();
                                                syncObject.Path = ApplicationSettingsManager.DefaultFexUserRootFolder;
                                                syncObject.Account = account;
                                                db.AccountSyncObjects.Add(syncObject);
                                            }

                                            syncObject.Token = cmd.Result;
                                            
                                            db.SaveChanges();
                                        });

                                        ((App)App.Current).ConfigureContainer();
                                        ((App)App.Current).ConfigureSyncWorkflow();
                                    }
                                };

                                authWindow.ShowDialog();
                            }
                        }
                        catch (ApiErrorException ex)
                        {
                            ex.Process();
                        }
                        catch (ConnectionException ex)
                        {
                            ex.Process();
                        }
                        catch (Exception ex)
                        {
                            ex.Process();
                        }
                    }
                };
            }
        }

        public ICommand SignOutCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => this.AccountSettingsExists && SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped,

                    CommandAction = () =>
                    {
                        try
                        {
                            var db = ((App)Application.Current).Container.Resolve<ISyncDataDbContext>();
                            db.LockedRun(() =>
                            {
                                db.AccountSyncObjects.RemoveRange(db.AccountSyncObjects);
                                db.Accounts.RemoveRange(db.Accounts);
                                db.SaveChanges();
                            });
                        }
                        catch (ApiErrorException ex)
                        {
                            ex.Process();
                        }
                        catch (ConnectionException ex)
                        {
                            ex.Process();
                        }
                        catch (Exception ex)
                        {
                            ex.Process();
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand AlertsShowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => this.AccountSettingsExists && SyncWorkflow.Singleton.Instance.Alerts.Any(),
                    CommandAction = () =>
                    {
                        var alert = SyncWorkflow.Singleton.Instance.Alerts.Where(a => a.Priority == Alert.AlertPriority.critical).FirstOrDefault();

                        if (alert != null)
                        {
                            //// Will use special windows for each alert type with critical priority
                            if (alert is CaptchaRequiredAlert captchaRequiredAlert)
                            {
                                AuthWindow w = new AuthWindow(captchaRequiredAlert.CaptchaRequestedEventArgs.Connection, captchaRequiredAlert.CaptchaRequestedEventArgs.CaptchaToken.Token);
                                if (w.ShowDialog() == true)
                                {
                                    captchaRequiredAlert.CaptchaRequestedEventArgs.CaptchaText = w.TxtCaptcha.Text;
                                    captchaRequiredAlert.MarkProcessed();
                                }
                            }
                            else
                            {
                                MessageBox.Show(alert.Text);
                                alert.MarkProcessed();
                            }

                            return;
                        }

                        AlertsWindow alertsWindow = new AlertsWindow(SyncWorkflow.Singleton.Instance.Alerts.OrderBy(item => item.Scope).ThenBy(item => item.Priority));
                        alertsWindow.ShowDialog();
                    }
                };
            }
        }

        public ICommand ShowMainWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (!this.AccountSettingsExists)
                        {
                            this.SignInCommand.Execute(null);
                        }
                        else if (SyncWorkflow.Singleton.Instance.Alerts.Any())
                        {
                            this.AlertsShowCommand.Execute(null);
                        }
                        else
                        {
                            this.ShowSettingsCommand.Execute(null);
                        }
                    }
                };
            }
        }

        public string SyncStatus
        {
            get
            {
                if (!this.AccountSettingsExists)
                {
                    return "Configuration required";
                }

                if (SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.WaitingForAlert)
                {
                    return "Notification";
                }

                if (SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Started)
                {
                    return "Sync";
                }

                if (SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped)
                {
                    return "Paused";
                }

                return SyncWorkflow.Singleton.Instance.Status.ToString();
            }
        }

        public void FireSyncStatusChanged()
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SyncStatus)));
        }

        public string ToolTipText
        {
            get
            {
                return "FexSync: Double-click for Settings, right-click for menu";
            }
        }
    }
}
