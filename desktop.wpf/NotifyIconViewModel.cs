﻿using desktop.common;
using net.fex.api.v1;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace desktop.wpf
{
    /// <summary>
    /// Provides bindable properties and commands for the NotifyIcon. In this sample, the
    /// view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
    /// in App.xaml.cs could have created this view model, and assigned it to the NotifyIcon.
    /// </summary>
    public class NotifyIconViewModel
    {
        private IConnection connect
        {
            get
            {
                return ((App)App.Current).Container.Get<IConnection>();
            }
        }

        public bool IsConnected => connect.IsSignedIn;

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
                return new DelegateCommand {CommandAction = () => Application.Current.Shutdown()};
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
                        var signInOrSignUpWindow = new SignIn();
                        try
                        {
                            if (signInOrSignUpWindow.ShowDialog() == true)
                            {
                                try
                                {
                                    var user = this.connect.SignIn("slutai", "100~`!@#$%^&*()[]{}:;\"',<.>/?+=-_", false);
                                }
                                catch (SignInException ex)
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
                        }
                        finally
                        {
                            signInOrSignUpWindow.Close();
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
                            this.connect.SignOut();
                        }
                        catch (SignInException ex)
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
    }
}
