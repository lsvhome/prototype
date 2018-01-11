using System;
using System.Collections.Generic;
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
                    CanExecuteFunc = () =>
                    {
                        SyncWorkflow.SyncWorkflowStatus[] allowedStatuses = new[]
                        {
                            SyncWorkflow.SyncWorkflowStatus.Started,
                            SyncWorkflow.SyncWorkflowStatus.Stopped,
                            SyncWorkflow.SyncWorkflowStatus.Idle,
                            SyncWorkflow.SyncWorkflowStatus.Indexing,
                            SyncWorkflow.SyncWorkflowStatus.Transferring
                        };

                        return this.AccountSettingsExists && allowedStatuses.Contains(SyncWorkflow.Singleton.Instance.Status);
                    },
                    CommandAction = () =>
                    {
                        try
                        {
                            var syncDb = ((App)App.Current).Container.Resolve<ISyncDataDbContext>();
                            var account = syncDb.Accounts.Single();

                            using (var conn = ((App)Application.Current).Container.Resolve<Data.IConnectionFactory>().CreateConnection(new Uri(ApplicationSettingsManager.ApiHost)))
                            {
                                conn.OnCaptchaUserInputRequired = (x, y) =>
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        AuthWindow w = new AuthWindow(y.Connection, y.CaptchaToken.Token);
                                        if (w.ShowDialog() == true)
                                        {
                                        }
                                    });
                                };

                                conn.SignIn(account.Login, account.Password, false);

                                SettingsWindow settings = new SettingsWindow(account, conn);

                                Application.Current.MainWindow = settings;

                                if (settings.ShowDialog() == true)
                                {
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
                    CanExecuteFunc = () =>
                    {
                        SyncWorkflow.SyncWorkflowStatus[] allowedStatuses = new[]
                        {
                            SyncWorkflow.SyncWorkflowStatus.Stopped
                        };

                        return this.AccountSettingsExists && allowedStatuses.Contains(SyncWorkflow.Singleton.Instance.Status);
                    },
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
                    CanExecuteFunc = () =>
                    {
                        SyncWorkflow.SyncWorkflowStatus[] allowedStatuses = new[]
                        {
                            SyncWorkflow.SyncWorkflowStatus.Started,
                            SyncWorkflow.SyncWorkflowStatus.Idle,
                            SyncWorkflow.SyncWorkflowStatus.Indexing,
                            SyncWorkflow.SyncWorkflowStatus.Transferring,
                            SyncWorkflow.SyncWorkflowStatus.WaitingForAlert
                        };

                        var ret = this.AccountSettingsExists && allowedStatuses.Contains(SyncWorkflow.Singleton.Instance.Status);
                        return ret;
                    },
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
                    CanExecuteFunc = () =>
                    {
                        SyncWorkflow.SyncWorkflowStatus[] allowedStatuses = new[]
                        {
                            SyncWorkflow.SyncWorkflowStatus.Stopped,
                        };

                        return !this.AccountSettingsExists && allowedStatuses.Contains(SyncWorkflow.Singleton.Instance.Status);
                    },

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

                                            db.SaveChanges();

                                            account.EnsureAccountHasDefaultSyncObject(db, conn);
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
                    CanExecuteFunc = () =>
                    {
                        SyncWorkflow.SyncWorkflowStatus[] allowedStatuses = new[]
                        {
                            SyncWorkflow.SyncWorkflowStatus.Stopped,
                        };

                        return this.AccountSettingsExists && allowedStatuses.Contains(SyncWorkflow.Singleton.Instance.Status);
                    },

                    CommandAction = () =>
                    {
                        try
                        {
                            var db = ((App)Application.Current).Container.Resolve<ISyncDataDbContext>();
                            db.LockedRun(() =>
                            {
                                var account = db.Accounts.Single();
                                db.RemoveAccountRecursive(account);
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

        public string IconPath { get; set; } = "/resources/default.ico";

        public string SyncStatusHeader
        {
            get
            {
                return $"Status: {this.SyncStatus}";
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
            System.Diagnostics.Trace.WriteLine($"Status3 = {this.SyncStatusHeader}");
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SyncStatusHeader)));
            switch (SyncWorkflow.Singleton.Instance.Status)
            {
                case SyncWorkflow.SyncWorkflowStatus.Idle:
                    this.IconPath = "/resources/done.ico";
                    break;
                case SyncWorkflow.SyncWorkflowStatus.WaitingForAlert:
                case SyncWorkflow.SyncWorkflowStatus.Stopped:
                    this.IconPath = "/resources/pause.ico";
                    break;
                case SyncWorkflow.SyncWorkflowStatus.Indexing:
                case SyncWorkflow.SyncWorkflowStatus.Transferring:
                    this.IconPath = "/resources/sync.ico";
                    break;
                default:
                    this.IconPath = "/resources/default.ico";
                    break;
            }

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IconPath)));
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
