using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autofac;
using Net.Fex.Api;

namespace FexSync.Data
{
    public partial class SyncWorkflow
    {
        private void Watcher_OnFolderDeleted(object sender, FolderDeletedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("Watcher_OnFolderDeleted");
            this.Task_RunSafe(() => { this.Watcher_OnFolderDeleted(e.FullPath); });
        }

        private void Watcher_OnFolderDeleted(string fullPath)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            syncDb.LockedRun(() =>
            {
                if (Directory.Exists(fullPath))
                {
                    throw new ApplicationException();
                }

                var syncObject = this.config.SyncObjects.Single(x => fullPath.Contains(x.Path));

                using (var cmd = new CommandPrepareLocalFolderDeleted(syncDb, fullPath, syncObject))
                {
                    cmd.Execute(this.connection);
                }
            });

            this.OnTransferFinished?.Invoke(this, new EventArgs());
        }
    }

    public class CommandPrepareLocalFolderDeleted : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private string DeletedFolderPath { get; set; }

        private AccountSyncObject SyncObject { get; set; }

        public CommandPrepareLocalFolderDeleted(ISyncDataDbContext context, string deletedFolderPath, AccountSyncObject syncObject) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(!File.Exists(deletedFolderPath), $"Folder {deletedFolderPath} still exists");
            this.SyncDb = context;
            this.DeletedFolderPath = deletedFolderPath;
            this.SyncObject = syncObject;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            var remoteFolder = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, this.DeletedFolderPath, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(remoteFolder != null, $"Folder {this.DeletedFolderPath} does not exists in db");

            connection.DeleteFile(this.SyncObject.Token, remoteFolder.UploadId);

            var deletedLocalSubFoldersAndFiles = this.SyncDb.LocalFiles.Where(item => item.Path.Contains(remoteFolder.Path)).ToList();

            foreach (var each in deletedLocalSubFoldersAndFiles)
            {
                this.SyncDb.LocalModifications.Add(new LocalFileModified { LocalFileOld = each, Path = each.Path });
                this.SyncDb.LocalFiles.Remove(each);
            }

            var deletedSubFoldersAndFiles = this.SyncDb.RemoteFiles.Where(item => item.Path.Contains(remoteFolder.Path)).ToList();
            foreach (var each in deletedSubFoldersAndFiles)
            {
                this.SyncDb.RemoteFiles.Remove(each);
            }

            bool itemStillExists = connection.Exists(this.SyncObject.Token, remoteFolder.UploadId, Path.GetFileName(this.DeletedFolderPath));

            if (itemStillExists)
            {
                throw new ApplicationException();
            }

            this.SyncDb.SaveChanges();
        }
    }
}
