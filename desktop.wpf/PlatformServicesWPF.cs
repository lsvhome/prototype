using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hardcodet.Wpf.TaskbarNotification;

namespace desktop.wpf
{
    public class PlatformServicesWPF : desktop.common.IPlatformServices
    {
        private TaskbarIcon notifyIcon;

        public void AddTrayIcon()
        {
            this.notifyIcon = (TaskbarIcon)App.Current.FindResource("NotifyIcon");
        }

        public void Dispose()
        {
            this.notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
        }
    }
}
