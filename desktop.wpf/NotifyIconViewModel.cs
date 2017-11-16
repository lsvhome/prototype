using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac;
using Desktop.Common;
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
                return ((App)App.Current).SyncWorkflow;
            }
        }

        public bool IsConnected => this.SyncWorkflow.Status == SyncWorkflow.SyncWorkflowStatus.Started;

        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand ShowSettingsCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => Application.Current.MainWindow == null,
                    CommandAction = () =>
                    {
                        if (Application.Current.MainWindow == null)
                        {
                            Application.Current.MainWindow = new MainWindow();
                        }

                        Application.Current.MainWindow.Show();
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
                    CanExecuteFunc = () => !this.IsConnected,
                    CommandAction = () =>
                    {
                        string login;
                        string password;
                        try
                        {
                            CredentialsManager.Load(out login, out password);
                            ((App)App.Current).SyncWorkflow.Start();
                        }
                        catch (Exception)
                        {
                            var authWindow = new AuthWindow();
                            if (authWindow.ShowDialog() == true)
                            {
                                ((App)App.Current).SyncWorkflow.Start();
                            }
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
                    CanExecuteFunc = () => this.IsConnected,

                    CommandAction = () =>
                    {
                        try
                        {
                            if (this.SyncWorkflow.Status != SyncWorkflow.SyncWorkflowStatus.Started)
                            {
                                throw new ApplicationException();
                            }

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

        public ICommand SignOutCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => true,

                    CommandAction = () =>
                    {
                        try
                        {
                            if (this.SyncWorkflow.Status != SyncWorkflow.SyncWorkflowStatus.Started)
                            {
                                throw new ApplicationException();
                            }

                            this.SyncWorkflow.Stop();

                            CredentialsManager.Clear();
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

        public string ToolTipText
        {
            get
            {
                return "Double-click for Settings, right-click for menu";
            }
        }
    }
}
