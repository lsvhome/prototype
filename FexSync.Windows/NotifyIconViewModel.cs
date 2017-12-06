using System;
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
    public class NotifyIconViewModel
    {
        private SyncWorkflow SyncWorkflow
        {
            get
            {
                if (!ApplicationSettingsManager.AccountSettings.Exists())
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
                    CanExecuteFunc = () => ApplicationSettingsManager.AccountSettings.Exists() && (SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Started || SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped),
                    CommandAction = () =>
                    {
                        try
                        {
                            SettingsWindow settings = new SettingsWindow();
                            Application.Current.MainWindow = settings;

                            settings.UserDataFolder = ApplicationSettingsManager.CurrentFexUserRootFolder;
                            if (settings.ShowDialog() == true)
                            {
                                var newFolder = settings.UserDataFolder;

                                //// required full restart
                                Task.Run(() =>
                                {
                                    var app = (App)Application.Current;
                                    if (app.SyncWorkflow != null)
                                    {
                                        bool doStopRecofigureStart = app.SyncWorkflow.IsBusy;

                                        EventHandler onStoppedSyncWorkflowEventHandler = null;

                                        onStoppedSyncWorkflowEventHandler = (sender, args) =>
                                        {
                                            Task.Run(() =>
                                            {
                                                ApplicationSettingsManager.CurrentFexUserRootFolder = newFolder;

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
                    CanExecuteFunc = () => ApplicationSettingsManager.AccountSettings.Exists() && SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped,
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
                    CanExecuteFunc = () => ApplicationSettingsManager.AccountSettings.Exists() && (SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Started || SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Starting),

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
                    CanExecuteFunc = () => !ApplicationSettingsManager.AccountSettings.Exists() && SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped,

                    CommandAction = () =>
                    {
                        try
                        {
                            using (var conn = new Connection(new Uri(ApplicationSettingsManager.ApiHost)))
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

                                        ApplicationSettingsManager.AccountSettings.Save(ApplicationSettingsManager.AccountSettings.AccountConfigFile);
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
                    CanExecuteFunc = () => ApplicationSettingsManager.AccountSettings.Exists() && SyncWorkflow.Singleton.Instance.Status == SyncWorkflow.SyncWorkflowStatus.Stopped,

                    CommandAction = () =>
                    {
                        try
                        {
                            ApplicationSettingsManager.AccountSettings.Clear();
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

        public ICommand ShowMainWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (!ApplicationSettingsManager.AccountSettings.Exists())
                        {
                            this.SignInCommand.Execute(null);
                        }
                        else
                        {
                            this.ShowSettingsCommand.Execute(null);
                        }
                    }
                };
            }
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
