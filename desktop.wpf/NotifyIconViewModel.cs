using desktop.common;
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

        private void ToggleRunningStatus(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)e.OriginalSource;
            menuItem.Header = "Pause";
        }

        public ICommand ConnectCommand
        {
            get
            {


                //return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
                return new DelegateCommand {
                    CanExecuteFunc = () => { return !this.IsConnected; },
                    CommandAction = () =>
                {

                    //Application.Current.MainWindow
                    var signInOrSignUpWindow = new SignIn();
                    bool shouldSignIn = signInOrSignUpWindow.ShowDialog() == true;
                    signInOrSignUpWindow.Close();

                    if (shouldSignIn)
                    {
                        IConnection conn = ((App)App.Current).Container.Get<IConnection>();
                        try
                        {
                            var user = conn.SignIn("slutai", "100~`!@#$%^&*()[]{}:;\"',<.>/?+=-_", false);
                        }
                        catch (Connection.LoginException ex)
                        {
                            ex.Process();
                            throw;
                        }
                        catch (Connection.ConnectionException ex)
                        {
                            ex.Process();
                            throw;
                        }
                        catch (Exception ex)
                        {
                            ex.Process();
                            throw;
                        }
                    }


                }

                };
            }
        }

        //public bool IsConnected { get; set; } = false;
        public bool IsConnected
        {
            get
            {
                IConnection conn = ((App)App.Current).Container.Get<IConnection>();
                var ret = conn.IsSignedIn;
                return ret;
            }
        }

        public Visibility ConnectVisibility
        {
            get
            {
                IConnection conn = ((App)App.Current).Container.Get<IConnection>();
                var ret = conn.IsSignedIn ? Visibility.Collapsed : Visibility.Visible;
                return ret;
            }
        }

        public ICommand DisconnectCommand
        {
            get
            {
                return new DelegateCommand {
                    CanExecuteFunc = () => { return this.IsConnected; },

                    CommandAction = () =>
                    //Application.Current.Shutdown()

                    {

                        //Application.Current.MainWindow
                        //var signInOrSignUpWindow = new SignIn();
                        //bool shouldSignIn = signInOrSignUpWindow.ShowDialog() == true;
                        //signInOrSignUpWindow.Close();

                        //if (shouldSignIn)
                        {
                            IConnection conn = ((App)App.Current).Container.Get<IConnection>();
                            try
                            {
                                conn.SignOut();
                            }
                            catch (Connection.LoginException ex)
                            {
                                ex.Process();
                                throw;
                            }
                            catch (Connection.ConnectionException ex)
                            {
                                ex.Process();
                                throw;
                            }
                            catch (Exception ex)
                            {
                                ex.Process();
                                throw;
                            }
                        }


                    }

                };
            }
        }

        public Visibility DisconnectVisibility
        {
            get
            {
                IConnection conn = ((App)App.Current).Container.Get<IConnection>();
                return conn.IsSignedIn ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }


    /// <summary>
    /// Simplistic delegate command for the demo.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        public Action CommandAction { get; set; }
        public Func<bool> CanExecuteFunc { get; set; }

        public void Execute(object parameter)
        {
            CommandAction();
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteFunc == null  || CanExecuteFunc();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
