using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace desktop.wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static desktop.common.IIocWrapper container = new desktop.common.IocWrapper();

        private TaskbarIcon notifyIcon;

        public App()
        {
            InitContainer();
        }

        public void InitContainer()
        {
            Dictionary<Type, object> mappings = new Dictionary<Type, object>();
            mappings.Add(typeof(desktop.common.IPlatformServices), new PlatformServicesWPF());
            container.Init(mappings);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon) FindResource("NotifyIcon");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }
    }
}
