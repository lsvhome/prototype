using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;

using FexSync;

namespace FexSync
{
    /// <summary>
    /// Interaction logic for QuickStartWindow.xaml
    /// </summary>
    public partial class QuickStartWindow : Window
    {
        private QuickStartImageList imageList = new QuickStartImageList();

        public QuickStartWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            this.Image.Source = this.imageList.GetCurrentImageSource();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public ICommand CommandNext
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => this.imageList.HasNext,
                    CommandAction = () =>
                    {
                        this.imageList.MoveNext();
                        this.Image.Source = this.imageList.GetCurrentImageSource();
                    }
                };
            }
        }

        public ICommand CommandBack
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => this.imageList.HasPrevious,
                    CommandAction = () =>
                    {
                        this.imageList.MoveBack();
                        this.Image.Source = this.imageList.GetCurrentImageSource();
                    }
                };
            }
        }
    }
}
