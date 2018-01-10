using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Autofac;
using FexSync.Data;
using Net.Fex.Api;

namespace FexSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Account Account { get; private set; }

        private IConnection Connection { get; set; }

        public SettingsWindow(Account account, IConnection connection)
        {
            this.Account = account;
            this.Connection = connection;
            this.InitializeComponent();
        }

        private void BtnAddSyncFolder_Click(object sender, RoutedEventArgs e)
        {
            AccountSyncObject[] alreadyUsedLocalSyncObjects = null;
            this.SyncDb.LockedRun(() =>
            {
                alreadyUsedLocalSyncObjects = this.SyncDb.AccountSyncObjects.ToArray();
            });

            var allRemoteObjects = this.Connection.ArchiveAll().ObjectList;
            CommandArchive.CommandArchiveResponseObject[] allowedObjects = allRemoteObjects.Where(z => !alreadyUsedLocalSyncObjects.Any(c => c.Token == z.Token)).ToArray();

            var addWindow = new AccountSyncObjectEditWindow(string.Empty, null, allowedObjects);
            addWindow.OnLocalFolderChanging += (senderWindow, args) =>
            {
                args.Allow = !alreadyUsedLocalSyncObjects.Any(d =>
                    d.Path.Contains(args.LocalPath.TrimEnd(System.IO.Path.DirectorySeparatorChar))
                    ||
                    args.LocalPath.Contains(d.Path.TrimEnd(System.IO.Path.DirectorySeparatorChar))
                    ||
                    string.Equals(args.LocalPath, d.Path, StringComparison.InvariantCultureIgnoreCase));
            };

            addWindow.OnServerObjectChanging += (x, y) => { /* Do nothing ? */ };

            if (addWindow.ShowDialog() == true)
            {
                var defaultSyncObject = new AccountSyncObject();
                defaultSyncObject.Account = Account;
                defaultSyncObject.Path = ApplicationSettingsManager.DefaultFexUserRootFolder;
                defaultSyncObject.Token = addWindow.SelKey;
                defaultSyncObject.Name = allowedObjects.Single(x => x.Token == addWindow.SelKey).Preview;

                this.SyncDb.LockedRun(() =>
                {
                    SyncDb.AccountSyncObjects.Add(defaultSyncObject);
                    SyncDb.SaveChanges();
                });

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.UserDataFolders)));
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

        private void DeleteSyncObjectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                AccountSyncObject obj = ((FrameworkElement)sender).DataContext as AccountSyncObject;

                this.SyncDb.LockedRun(() =>
                {
                    var syncObject = this.SyncDb.AccountSyncObjects.Single(x => x.AccountSyncObjectId == obj.AccountSyncObjectId);
                    this.SyncDb.RemoveAccountSyncObjectRecursive(syncObject);
                    this.SyncDb.SaveChanges();
                });

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.UserDataFolders)));
            }
            catch (Exception exception)
            {
                exception.Process();
                throw;
            }
        }

        public ISyncDataDbContext SyncDb
        {
            get
            {
                var ret = ((App)Application.Current).Container.Resolve<ISyncDataDbContext>();
                return ret;
            }
        }

        private readonly ObservableCollection<AccountSyncObject> userDataFolders = new ObservableCollection<AccountSyncObject>();

        public ObservableCollection<AccountSyncObject> UserDataFolders
        {
            get
            {
                this.SyncDb.LockedRun(() =>
                {
                    this.userDataFolders.Clear();
                    foreach (var folderDefinition in this.SyncDb.AccountSyncObjects.Where(x => x.Account == Account))
                    {
                        userDataFolders.Add(folderDefinition);
                    }
                });

                return this.userDataFolders;
            }

            set
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.UserDataFolders)));
            }
        }
    }
}
