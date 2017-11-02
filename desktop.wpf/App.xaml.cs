using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace desktop.wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static desktop.common.IIocWrapper container = new desktop.common.IocWrapper();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Dictionary<Type, object> mappings = new Dictionary<Type, object>();
            mappings.Add(typeof(desktop.common.IPlatformServices), new PlatformServicesWPF());
            container.Init(mappings);
        }
    }
}
