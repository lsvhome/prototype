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
        private ThreadSafeListWithLock<Task> scheduledTasks = new ThreadSafeListWithLock<Task>();

        private void Task_RunSafe(Action action)
        {
            Task t = Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    ex.Process();
                }
            });

            this.scheduledTasks.RemoveAll(task => task.IsCompleted);
            this.scheduledTasks.Add(t);
        }

        private void Watcher_OnFileDeleted(object sender, FileDeletedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("Watcher_OnFileDeleted");
            this.Task_RunSafe(() => { this.Watcher_OnFileDeleted(e.FullPath); });
        }

        private void Watcher_OnFileDeleted(string fullPath)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            syncDb.LockedRun(() =>
            {
                if (File.Exists(fullPath))
                {
                    throw new ApplicationException();
                }

                using (var cmd = new CommandPrepareLocalFileDeleted(syncDb, fullPath, this.config.AccountSettings.AccountDataFolder))
                {
                    cmd.Execute(this.connection);
                }
            });

            this.OnTransferFinished?.Invoke(this, new EventArgs());
        }
    }

    public class CommandPrepareLocalFileDeleted : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private string DeletedFilePath { get; set; }

        private string AccountDataFolder { get; set; }

        public CommandPrepareLocalFileDeleted(ISyncDataDbContext context, string deletedFilePath, string accountDataFolder) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(!File.Exists(deletedFilePath), $"File {deletedFilePath} still exists");
            this.SyncDb = context;
            this.DeletedFilePath = deletedFilePath;
            this.AccountDataFolder = accountDataFolder;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            System.Diagnostics.Debug.Assert(!this.SyncDb.LocalFiles.Any(x => string.Equals(x.Path, this.DeletedFilePath, StringComparison.InvariantCultureIgnoreCase)), $"File {this.DeletedFilePath} already indexed");

            var relativePath = this.DeletedFilePath.Replace(this.AccountDataFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar);

            var localFile = this.SyncDb.LocalFiles.SingleOrDefault(x => string.Equals(x.Path, relativePath, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(localFile != null, $"File {this.DeletedFilePath} have not been found");

            var remoteFile = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, relativePath, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(remoteFile != null, $"File {this.DeletedFilePath} does not exists in db");

            var remoteFileFolder = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, Path.GetDirectoryName(relativePath), StringComparison.InvariantCultureIgnoreCase));

            var objectToken = localFile.Path.Split(Path.DirectorySeparatorChar).First();

            this.SyncDb.LocalModifications.Add(new LocalFileModified { LocalFileOld = localFile, Path = localFile.Path });
            this.SyncDb.LocalFiles.Remove(localFile);

            this.SyncDb.RemoteModifications.Add(new RemoteFileModified { RemoteFileOld = remoteFile, Path = remoteFile.Path });
            this.SyncDb.RemoteFiles.Remove(remoteFile);

            connection.DeleteFile(objectToken, remoteFile.UploadId);

            bool fileStillExists = connection.Exists(objectToken, remoteFileFolder?.UploadId, Path.GetFileName(this.DeletedFilePath));

            if (fileStillExists)
            {
                throw new ApplicationException();
            }

            this.SyncDb.SaveChanges();
        }
    }
}
