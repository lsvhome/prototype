using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hardcodet.Wpf.TaskbarNotification;

namespace Desktop.Wpf
{
    public class PlatformServicesWPF : Desktop.Common.IPlatformServices
    {
        public TaskbarIcon NotifyIcon { get; set; }

        public void AddTrayIcon()
        {
            this.NotifyIcon = (TaskbarIcon)App.Current.FindResource("NotifyIcon");
            this.NotifyIcon.DataContext = new NotifyIconViewModel();
        }

        public void SetTrayIconStatusConnected()
        {
        }

        public void SetTrayIconStatusDisconnected()
        {
        }

        public void Dispose()
        {
            this.NotifyIcon.Dispose(); //// the icon would clean up automatically, but this is cleaner
        }
    }
}
