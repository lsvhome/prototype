using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FexSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            this.InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void BtnCurrentDataFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (System.IO.Directory.Exists(this.TxtCurrentDataFolder.Text))
                {
                    dialog.SelectedPath = this.TxtCurrentDataFolder.Text;
                }

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var di = new System.IO.DirectoryInfo(dialog.SelectedPath);

                    if (!di.Exists)
                    {
                        MessageBox.Show($"Folder {dialog.SelectedPath} does not exists.");
                        return;
                    }

                    if (di.GetFileSystemInfos().Count() > 0)
                    {
                        MessageBox.Show($"Folder {dialog.SelectedPath} isn't empty.");
                        return;
                    }

                    this.TxtCurrentDataFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public string UserDataFolder
        {
            get
            {
                return this.TxtCurrentDataFolder.Text;
            }

            set
            {
                this.TxtCurrentDataFolder.Text = value;
            }
        }
    }
}
