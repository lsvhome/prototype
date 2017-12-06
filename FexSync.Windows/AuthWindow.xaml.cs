using System;
using System.Configuration;
using System.Linq;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Autofac;
using Net.Fex.Api;

namespace FexSync
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        /*
        private Net.Fex.Api.IConnection Connect
        {
            get
            {
                return ((App)App.Current).Container.Resolve<IConnectionFactory>().CreateConnection();
            }
        }
        */
        private const string LoginPlaceholder = "Логин или телефон";
        private const string CaptchaPlaceholder = "Символы с картинки";

        private readonly AutoResetEvent waitForCaptchaEvent = new AutoResetEvent(false);

        private readonly Color defaultBorderColor = Color.FromRgb(255, 255, 255);
        private readonly Color focusedBorderColor = Color.FromRgb(66, 164, 245);
        private readonly Color errorBorderColor = Color.FromRgb(255, 82, 82);

        private readonly string captchaUrl = ConfigurationManager.AppSettings["FEX.NET.ApiHost"] + "captcha?captcha_token=";
        private readonly RequestCachePolicy requestCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

        private string captchaToken;

        private IConnection connection = null;

        public AuthWindow(IConnection conn)
        {
            this.connection = conn;

            this.InitializeComponent();

            this.LnkRegister.RequestNavigate += this.Link_RequestNavigate;
            this.LnkRecoverPassword.RequestNavigate += this.Link_RequestNavigate;

            this.Initialize();
        }

        public void Initialize()
        {
            /*
            string login;
            try
            {
                login = await _program.Initialize();
            }
            catch
            {
                if (Settings.IsActive) ((App)Application.Current).StartStopUpload(false);

                ((App)Application.Current).ShowRestoreMenuItem();
                ((App)Application.Current).ShowBalloonTip("FEX Backup", "Нет связи с сервером.", System.Windows.Forms.ToolTipIcon.Error);
                Hide();
                return;
            }

            Settings.Login = login;

            if (string.IsNullOrEmpty(login))
            {
                AppShortcut.AutoStart(false);
                Show();
            }
            else
            {
                TxtLogin.Text = string.Empty;
                TxtLogin_LostFocus();

                PwdPassword.Password = string.Empty;
                PwdPassword_LostFocus();

                ((App)Application.Current).ShowAllMenuItems();

                if (IsVisible)
                {
                    if (ClickOnceHelper.IsApplicationNetworkDeployed && Settings.IsAutoStartEnabled && !AppShortcut.IsAutoStartEnabled)
                        AppShortcut.AutoStart(true);

                    Hide();

                    ((App)Application.Current).ShowSettingsWindow();
                }
            }
            */
        }

        private void ShowCaptcha()
        {
            this.GetCaptchaImage();

            this.GrdLogin.Visibility = Visibility.Hidden;
            this.GrdCaptcha.Visibility = Visibility.Visible;
        }

        private void HideCaptcha()
        {
            this.GrdCaptcha.Visibility = Visibility.Hidden;
            this.GrdLogin.Visibility = Visibility.Visible;
        }

        private void ClearCaptcha()
        {
            this.TxtCaptcha.Text = string.Empty;
            this.ImgCaptcha.Source = null;
        }

        private void GetCaptchaImage()
        {
            this.captchaToken = Guid.NewGuid().ToString("N");
            this.ImgCaptcha.Source = new BitmapImage(new Uri(this.captchaUrl + this.captchaToken), this.requestCachePolicy);

            this.TxtCaptcha.Text = string.Empty;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            this.HideError();

            var login = TxtLogin.Text.Trim().TrimStart('+');
            if (login.Length == 0 || login == LoginPlaceholder)
            {
                this.ShowError("Введите логин или телефон.");
                return;
            }

            if (PwdPassword.Password.Length < 8 || Regex.IsMatch(PwdPassword.Password, "[а-яёєїі]"))
            {
                this.ShowError("Неверный логин или пароль.");
                return;
            }

            if (!string.IsNullOrEmpty(this.captchaToken))
            {
                this.ShowCaptcha();
                return;
            }

            Task.Run(() => this.SignInAsync(login, PwdPassword.Password, this.connection));
        }

        private async Task<bool> IsCredentialsValidAsync(IConnection conn, string login, string password)
        {
            try
            {
                await conn.SignInAsync(login, password, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task SignInAsync(string login, string password, IConnection conn)
        {
            conn.OnCaptchaUserInputRequired = this.Connect_OnCaptchaUserInputRequired;
            try
            {
                if (await this.IsCredentialsValidAsync(conn, login, password))
                {
                    if (this.OnSignedIn != null)
                    {
                        this.OnSignedIn(this, new CommandSignIn.SignInEventArgs(conn, login, password));
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        this.DialogResult = true;
                        this.Close();
                    });
                }
            }
            catch (Net.Fex.Api.ApiErrorException ex)
            {
                this.Dispatcher.Invoke(() => { this.ShowError(ex.Message); });
            }
            catch (Net.Fex.Api.CaptchaRequiredException)
            {
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() => { this.ShowError(ex.Message); });
            }
            finally
            {
                this.captchaToken = null;
                conn.OnCaptchaUserInputRequired = null;
            }
        }

        private async Task SignInAsync(string login, string password)
        {
            using (var conn = ((App)App.Current).Container.Resolve<Data.IConnectionFactory>().CreateConnection(new Uri(ApplicationSettingsManager.ApiHost)))
            {
                await this.SignInAsync(login, password, conn);
            }
        }

        private void Connect_OnCaptchaUserInputRequired(object sender, Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                captchaToken = e.CaptchaToken.Token;
                this.ImgCaptcha.Source = new BitmapImage(new Uri(captchaUrl + e.CaptchaToken.Token), requestCachePolicy);

                this.TxtCaptcha.Text = string.Empty;

                this.GrdLogin.Visibility = Visibility.Hidden;
                this.GrdCaptcha.Visibility = Visibility.Visible;
            });

            this.waitForCaptchaEvent.Reset();
            this.waitForCaptchaEvent.WaitOne();

            this.Dispatcher.Invoke(() =>
            {
                e.CaptchaText = this.TxtCaptcha.Text;
                this.GrdLogin.Visibility = Visibility.Visible;
                this.GrdCaptcha.Visibility = Visibility.Hidden;
                this.UpdateLayout();
            });
        }

        private void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void HideError()
        {
            this.TxbError.Visibility = Visibility.Hidden;
            this.TxbError.Text = string.Empty;
        }

        private void ShowError(string message)
        {
            this.TxbError.Text = message;
            this.TxbError.Visibility = Visibility.Visible;
            this.ShowErrorHint();
        }

        private void ShowErrorHint()
        {
            var borderBrush = Resources["TextBox.Error.Border"] as Brush;
            this.TxtLogin.BorderBrush = borderBrush;
            this.TxtPassword.BorderBrush = borderBrush;
            this.PwdPassword.BorderBrush = borderBrush;
        }

        private void HideErrorHint()
        {
            var borderBrush = Resources["TextBox.Static.Border"] as Brush;
            this.TxtLogin.BorderBrush = borderBrush;
            this.TxtPassword.BorderBrush = borderBrush;
            this.PwdPassword.BorderBrush = borderBrush;
        }

        private void ShowCaptchaError(string message)
        {
            this.TxbCaptchaError.Text = message;
            this.TxbCaptchaError.Visibility = Visibility.Visible;
            this.ShowCaptchaErrorHint();
        }

        private void HideCaptchaError()
        {
            this.TxbCaptchaError.Visibility = Visibility.Hidden;
            this.TxbCaptchaError.Text = string.Empty;
        }

        private void ShowCaptchaErrorHint()
        {
            this.TxtCaptcha.BorderBrush = Resources["TextBox.Error.Border"] as Brush;
        }

        private void HideCaptchaErrorHint()
        {
            this.TxtCaptcha.BorderBrush = Resources["TextBox.Static.Border"] as Brush;
        }

        private void TxtLogin_GotFocus(object sender, RoutedEventArgs e)
        {
            this.HideErrorHint();
            this.TxtLogin_GotFocus();
        }

        private void TxtLogin_GotFocus()
        {
            if (this.TxtLogin.Text == LoginPlaceholder)
            {
                this.TxtLogin.Text = string.Empty;
                this.TxtLogin.Foreground = Resources["TextBox.Focus.Foreground"] as Brush;
            }
        }

        private void TxtLogin_LostFocus(object sender, RoutedEventArgs e)
        {
            this.TxtLogin_LostFocus();
        }

        private void TxtLogin_LostFocus()
        {
            if (this.TxtLogin.Text == string.Empty)
            {
                this.TxtLogin.Text = LoginPlaceholder;
                this.TxtLogin.Foreground = Resources["TextBox.Static.Foreground"] as Brush;
            }
        }

        private void TxtCaptcha_GotFocus(object sender, RoutedEventArgs e)
        {
            this.HideCaptchaErrorHint();

            if (this.TxtCaptcha.Text == CaptchaPlaceholder)
            {
                this.TxtCaptcha.Text = string.Empty;
                this.TxtCaptcha.Foreground = Resources["TextBox.Focus.Foreground"] as Brush;
            }
        }

        private void TxtCaptcha_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.TxtCaptcha.Text == string.Empty)
            {
                this.TxtCaptcha.Text = CaptchaPlaceholder;
                this.TxtCaptcha.Foreground = Resources["TextBox.Static.Foreground"] as Brush;
            }
        }

        private void TxtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            this.HideErrorHint();
            this.TxtPassword_GotFocus();
        }

        private void TxtPassword_GotFocus()
        {
            this.TxtPassword.Visibility = Visibility.Hidden;
            this.PwdPassword.Visibility = Visibility.Visible;
            this.PwdPassword.Focus();
        }

        private void PwdPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            this.PwdPassword_LostFocus();
        }

        private void PwdPassword_LostFocus()
        {
            if (string.IsNullOrEmpty(PwdPassword.Password))
            {
                this.PwdPassword.Visibility = Visibility.Hidden;
                this.TxtPassword.Visibility = Visibility.Visible;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.GrdCaptcha.Visibility = Visibility.Hidden;
            this.GrdLogin.Visibility = Visibility.Visible;
        }

        private void BtnCaptcha_Click(object sender, RoutedEventArgs e)
        {
            this.HideCaptchaError();

            if (string.IsNullOrWhiteSpace(this.TxtCaptcha.Text) || this.TxtCaptcha.Text == CaptchaPlaceholder)
            {
                this.ShowCaptchaError("Введите текст с картинки.");
            }

            var login = this.TxtLogin.Text.Trim().TrimStart('+');

            this.waitForCaptchaEvent.Set();
        }

        public event EventHandler<CommandSignIn.SignInEventArgs> OnSignedIn;
    }
}