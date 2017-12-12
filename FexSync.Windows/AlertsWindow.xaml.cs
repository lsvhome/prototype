using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using FexSync.Data;

namespace FexSync
{
    /// <summary>
    /// Interaction logic for AlertsWindow.xaml
    /// </summary>
    public partial class AlertsWindow : Window
    {
        private IEnumerable<Data.Alert> alerts;

        public AlertsWindow(IEnumerable<Data.Alert> alerts)
        {
            this.InitializeComponent();
            this.alerts = alerts;

            this.lixtBoxAlerts.MouseDoubleClick += this.LixtBoxAlerts_MouseDoubleClick;

            this.lixtBoxAlerts.ItemsSource = this.alerts;
        }

        private void LixtBoxAlerts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.lixtBoxAlerts.SelectedItem is Data.Alert alert)
            {
                if (alert is CaptchaRequiredAlert captchaRequiredAlert)
                {
                    AuthWindow authWindow = new AuthWindow(captchaRequiredAlert.CaptchaRequestedEventArgs.Connection, captchaRequiredAlert.CaptchaRequestedEventArgs.CaptchaToken.Token);
                    if (authWindow.ShowDialog() == true)
                    {
                        captchaRequiredAlert.CaptchaRequestedEventArgs.CaptchaText = authWindow.TxtCaptcha.Text;
                        captchaRequiredAlert.MarkProcessed();
                    }
                }
                else
                {
                    MessageBox.Show(alert.Text);
                    alert.MarkProcessed();
                }
            }
        }
    }
}
