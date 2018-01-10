using System;
using System.Collections.Generic;
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
using Net.Fex.Api;

namespace FexSync
{
    /// <summary>
    /// Interaction logic for AccountSyncObjectEditWindow.xaml
    /// </summary>
    public partial class AccountSyncObjectEditWindow : Window
    {
        public class ServerObjectChangingEventArgs : EventArgs
        {
            public bool Allow { get; set; } = true;

            public string Token { get; set; }

            public string LocalPath { get; set; }
        }

        private string userDataFolderInitial;

        private string syncObjectTokenInitial;

        public CommandArchive.CommandArchiveResponseObject[] AllowedObjects { get; private set; }

        public string SelKey { get; set; }
        
        public AccountSyncObjectEditWindow(string path, string syncObjectToken, CommandArchive.CommandArchiveResponseObject[] allowedObjects)
        {
            this.userDataFolderInitial = path;
            this.syncObjectTokenInitial = syncObjectToken;
            this.AllowedObjects = allowedObjects;
            this.SelKey = allowedObjects.SingleOrDefault(x => x.Token == this.syncObjectTokenInitial)?.Token;

            this.InitializeComponent();

            this.TxtLocalFolder.Text = this.userDataFolderInitial;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool HasValue
        {
            get { return !string.IsNullOrWhiteSpace(this.TxtLocalFolder.Text) && this.ComboBoxServerObject.SelectionBoxItem != null; }
        }

        public event EventHandler<ServerObjectChangingEventArgs> OnLocalFolderChanging;

        public event EventHandler<ServerObjectChangingEventArgs> OnServerObjectChanging;

        private void BtnLocalFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (System.IO.Directory.Exists(this.TxtLocalFolder.Text))
                {
                    dialog.SelectedPath = this.TxtLocalFolder.Text;
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

                    var a = new ServerObjectChangingEventArgs { LocalPath = dialog.SelectedPath, Token = this.SelKey };
                    this.OnLocalFolderChanging?.Invoke(this, a);
                    if (!a.Allow)
                    {
                        return;
                    }

                    this.TxtLocalFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void ComboBoxServerObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var args = new ServerObjectChangingEventArgs { LocalPath = this.TxtLocalFolder.Text, Token = this.SelKey };
            this.OnServerObjectChanging?.Invoke(this, args);
            if (!args.Allow)
            {
                this.SelKey = this.AllowedObjects.SingleOrDefault(x => x.Token == this.syncObjectTokenInitial)?.Token;
            }
        }
    }
}