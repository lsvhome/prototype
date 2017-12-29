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

        private Action SafeAction(Action action)
        {
            return () =>
            {
                try
                {
                    action();
                }
                catch (OperationCanceledException)
                {
                    //// do nothing - suppress exception
                }
                catch (Exception ex)
                {
                    ex.Process();

                    this.OnException?.Invoke(this, new ExceptionEventArgs(ex));

                    System.Diagnostics.Debug.Fail(ex.ToString());
                }
            };
        }

        private Task SafeTask(Action action)
        {
            return new Task(this.SafeAction(action));
        }

        private Task Task_RunSafe(Action action)
        {
            this.scheduledTasks.RemoveAll(eachTask => eachTask.IsCompleted);

            Task task = this.SafeTask(action);
            this.scheduledTasks.Add(task);
            task.Start();
            return task;
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

                var syncObject = this.config.SyncObjects.Single(x => fullPath.Contains(x.Path));

                using (var cmd = new CommandPrepareLocalFileDeleted(syncDb, fullPath, syncObject))
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

        private AccountSyncObject SyncObject { get; set; }

        public CommandPrepareLocalFileDeleted(ISyncDataDbContext context, string deletedFilePath, AccountSyncObject syncObject) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(!File.Exists(deletedFilePath), $"File {deletedFilePath} still exists");
            this.SyncDb = context;
            this.DeletedFilePath = deletedFilePath;
            this.SyncObject = syncObject;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            System.Diagnostics.Debug.Assert(!this.SyncDb.LocalFiles.Any(x => string.Equals(x.Path, this.DeletedFilePath, StringComparison.InvariantCultureIgnoreCase)), $"File {this.DeletedFilePath} already indexed");

            var relativePath = this.DeletedFilePath.Replace(this.SyncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

            var localFile = this.SyncDb.LocalFiles.SingleOrDefault(x => string.Equals(x.Path, relativePath, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(localFile != null, $"File {this.DeletedFilePath} have not been found");

            var remoteFile = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, relativePath, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(remoteFile != null, $"File {this.DeletedFilePath} does not exists in db");

            var remoteFileFolder = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, Path.GetDirectoryName(relativePath), StringComparison.InvariantCultureIgnoreCase));

            this.SyncDb.LocalModifications.Add(new LocalFileModified { LocalFileOld = localFile, Path = localFile.Path });
            this.SyncDb.LocalFiles.Remove(localFile);

            this.SyncDb.RemoteModifications.Add(new RemoteFileModified { RemoteFileOld = remoteFile, Path = remoteFile.Path });
            this.SyncDb.RemoteFiles.Remove(remoteFile);

            connection.DeleteFile(this.SyncObject.Token, remoteFile.UploadId);

            bool fileStillExists = connection.Exists(this.SyncObject.Token, remoteFileFolder?.UploadId, Path.GetFileName(this.DeletedFilePath));

            if (fileStillExists)
            {
                throw new ApplicationException();
            }

            this.SyncDb.SaveChanges();
        }
    }
}
