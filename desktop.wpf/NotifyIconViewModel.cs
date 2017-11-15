using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac;
using Desktop.Common;
using Net.Fex.Api;

namespace Desktop.Wpf
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
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => Application.Current.MainWindow == null,
                    CommandAction = () =>
                    {
                        Application.Current.MainWindow = new MainWindow();
                        Application.Current.MainWindow.Show();
                    }
                };
            }
        }

        /// <summary>
        /// Hides the main window. This command is only enabled if a window is open.
        /// </summary>
        public ICommand HideWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => Application.Current.MainWindow.Close(),
                    CanExecuteFunc = () => Application.Current.MainWindow != null
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

        public string ToolTipText { get{ return "Double-click for Settings, right-click for menu"; } }
    }
}
