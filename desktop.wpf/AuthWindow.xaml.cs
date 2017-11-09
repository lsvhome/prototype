using System;
using System.Configuration;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FEX.Backup.ClickOnce;

namespace FEX.Backup
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        private readonly IProgram _program;

        private readonly Color DefaultBorderColor = Color.FromRgb(255, 255, 255);
        private readonly Color FocusedBorderColor = Color.FromRgb(66, 164, 245);
        private readonly Color ErrorBorderColor = Color.FromRgb(255, 82, 82);

        private const string LoginPlaceholder = "Логин или телефон";
        private const string CaptchaPlaceholder = "Символы с картинки";
        private readonly string captchaUrl = ConfigurationManager.AppSettings["FEX.NET.ApiHost"] + "captcha?captcha_token=";
        private readonly RequestCachePolicy requestCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

        private string _captchaToken;

        public AuthWindow()
        {
            InitializeComponent();

            LnkRegister.RequestNavigate += Link_RequestNavigate;
            LnkRecoverPassword.RequestNavigate += Link_RequestNavigate;
            _program = IoC.Container.GetInstance<IProgram>();

            Initialize();
        }

        public async void Initialize()
        {
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
        }

        private void ShowCaptcha()
        {
            GetCaptchaImage();

            GrdLogin.Visibility = Visibility.Hidden;
            GrdCaptcha.Visibility = Visibility.Visible;
        }

        private void HideCaptcha()
        {
            GrdCaptcha.Visibility = Visibility.Hidden;
            GrdLogin.Visibility = Visibility.Visible;
        }

        private void ClearCaptcha()
        {
            TxtCaptcha.Text = string.Empty;
            ImgCaptcha.Source = null;
        }

        private void GetCaptchaImage()
        {
            _captchaToken = Guid.NewGuid().ToString("N");
            ImgCaptcha.Source = new BitmapImage(new Uri(captchaUrl + _captchaToken), requestCachePolicy);

            TxtCaptcha.Text = string.Empty;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            var login = TxtLogin.Text.Trim().TrimStart('+');
            if (login.Length == 0 || login == LoginPlaceholder)
            {
                ShowError("Введите логин или телефон.");
                return;
            }

            if (PwdPassword.Password.Length < 8 || Regex.IsMatch(PwdPassword.Password, "[а-яёєїі]"))
            {
                ShowError("Неверный логин или пароль.");
                return;
            }

            if (!string.IsNullOrEmpty(_captchaToken))
            {
                ShowCaptcha();
                return;
            }

            var signin = await _program.Signin(login, PwdPassword.Password);
            if (signin.Captcha)
                ShowCaptcha();
            else if (!signin.Result)
                ShowError(signin.Error?.Message);
            else if (signin.User?.Info?.MaxUploadSize > 0)
                Initialize();
            else
                ShowError("Приложение доступно только для пользователей с пакетом FEX Plus.");
        }

        private void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void HideError()
        {
            TxbError.Visibility = Visibility.Hidden;
            TxbError.Text = string.Empty;
        }

        private void ShowError(string message)
        {
            TxbError.Text = message;
            TxbError.Visibility = Visibility.Visible;
            ShowErrorHint();
        }

        private void ShowErrorHint()
        {
            var borderBrush = Resources["TextBox.Error.Border"] as Brush;
            TxtLogin.BorderBrush = borderBrush;
            TxtPassword.BorderBrush = borderBrush;
            PwdPassword.BorderBrush = borderBrush;
        }

        private void HideErrorHint()
        {
            var borderBrush = Resources["TextBox.Static.Border"] as Brush;
            TxtLogin.BorderBrush = borderBrush;
            TxtPassword.BorderBrush = borderBrush;
            PwdPassword.BorderBrush = borderBrush;
        }

        private void ShowCaptchaError(string message)
        {
            TxbCaptchaError.Text = message;
            TxbCaptchaError.Visibility = Visibility.Visible;
            ShowCaptchaErrorHint();
        }

        private void HideCaptchaError()
        {
            TxbCaptchaError.Visibility = Visibility.Hidden;
            TxbCaptchaError.Text = string.Empty;
        }

        private void ShowCaptchaErrorHint()
        {
            TxtCaptcha.BorderBrush = Resources["TextBox.Error.Border"] as Brush;
        }

        private void HideCaptchaErrorHint()
        {
            TxtCaptcha.BorderBrush = Resources["TextBox.Static.Border"] as Brush;
        }

        private void TxtLogin_GotFocus(object sender, RoutedEventArgs e)
        {
            HideErrorHint();
            TxtLogin_GotFocus();
        }

        private void TxtLogin_GotFocus()
        {
            if (TxtLogin.Text == LoginPlaceholder)
            {
                TxtLogin.Text = string.Empty;
                TxtLogin.Foreground = Resources["TextBox.Focus.Foreground"] as Brush;
            }
        }

        private void TxtLogin_LostFocus(object sender, RoutedEventArgs e)
        {
            TxtLogin_LostFocus();
        }

        private void TxtLogin_LostFocus()
        {
            if (TxtLogin.Text == string.Empty)
            {
                TxtLogin.Text = LoginPlaceholder;
                TxtLogin.Foreground = Resources["TextBox.Static.Foreground"] as Brush;
            }
        }

        private void TxtCaptcha_GotFocus(object sender, RoutedEventArgs e)
        {
            HideCaptchaErrorHint();

            if (TxtCaptcha.Text == CaptchaPlaceholder)
            {
                TxtCaptcha.Text = string.Empty;
                TxtCaptcha.Foreground = Resources["TextBox.Focus.Foreground"] as Brush;
            }
        }

        private void TxtCaptcha_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TxtCaptcha.Text == string.Empty)
            {
                TxtCaptcha.Text = CaptchaPlaceholder;
                TxtCaptcha.Foreground = Resources["TextBox.Static.Foreground"] as Brush;
            }
        }

        private void TxtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            HideErrorHint();
            TxtPassword_GotFocus();
        }

        private void TxtPassword_GotFocus()
        {
            TxtPassword.Visibility = Visibility.Hidden;
            PwdPassword.Visibility = Visibility.Visible;
            PwdPassword.Focus();
        }

        private void PwdPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            PwdPassword_LostFocus();
        }

        private void PwdPassword_LostFocus()
        {
            if (string.IsNullOrEmpty(PwdPassword.Password))
            {
                PwdPassword.Visibility = Visibility.Hidden;
                TxtPassword.Visibility = Visibility.Visible;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            GrdCaptcha.Visibility = Visibility.Hidden;
            GrdLogin.Visibility = Visibility.Visible;
        }

        private async void BtnCaptcha_Click(object sender, RoutedEventArgs e)
        {
            HideCaptchaError();

            if (string.IsNullOrWhiteSpace(TxtCaptcha.Text) || TxtCaptcha.Text == CaptchaPlaceholder)
                ShowCaptchaError("Введите текст с картинки.");

            var login = TxtLogin.Text.Trim().TrimStart('+');

            var signin = await _program.Signin(login, PwdPassword.Password, _captchaToken, TxtCaptcha.Text);
            if (!signin.Result && signin.Error?.Id == (int)API.ErrorType.WrongCaptcha)
            {
                GetCaptchaImage();
                ShowCaptchaError(signin.Error?.Message);
            }
            else
            {
                if (signin.Captcha)
                    GetCaptchaImage();

                HideCaptcha();

                if (!signin.Result)
                    ShowError(signin.Error?.Message);
                else if (signin.User?.Info?.MaxUploadSize > 0)
                    Initialize();
                else
                    ShowError("Приложение доступно только для пользователей с пакетом FEX Plus.");
            }
        }
    }
}