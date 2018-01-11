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
        private void Watcher_OnFileMoved(object sender, FileMovedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("Watcher_OnFileMoved");
            this.Task_RunSafe(() => { this.Watcher_OnFileMoved(e.OldPath, e.NewPath); });
        }

        private void Watcher_OnFileMoved(string oldPath, string newPath)
        {
            try
            {
                ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
                syncDb.LockedRun(() =>
                {
                    var fiOld = new FileInfo(oldPath);
                    var fiNew = new FileInfo(newPath);

                    if (fiOld.Exists)
                    {
                        throw new ApplicationException();
                    }

                    if (!fiNew.Exists)
                    {
                        throw new ApplicationException();
                    }

                    //// check file is accessible
                    using (fiNew.OpenWrite())
                    {
                    }

                    using (var cmd = new CommandPrepareLocalFileMoved(syncDb, fiOld, fiNew))
                    {
                        cmd.Execute(this.connection);
                    }
                });

                this.OnTransferFinished?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                ex.Process();
            }
        }
    }

    public class CommandPrepareLocalFileMoved : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private FileInfo DeletedFile { get; set; }

        private FileInfo CreatedFile { get; set; }

        public CommandPrepareLocalFileMoved(ISyncDataDbContext context, FileInfo deletedFile, FileInfo createdFile) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(createdFile.Exists, $"File {createdFile.FullName} does not exists");
            this.SyncDb = context;
            this.DeletedFile = deletedFile;
            this.CreatedFile = createdFile;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            var syncObjectDeleted = this.SyncDb.AccountSyncObjects.Single(x => this.DeletedFile.FullName.Contains(x.Path));
            var syncObjectCreated = this.SyncDb.AccountSyncObjects.Single(x => this.CreatedFile.FullName.Contains(x.Path));

            var relativeOldFilePath = this.DeletedFile.FullName.Replace(syncObjectDeleted.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            var localOldFile = this.SyncDb.LocalFiles.SingleOrDefault(x => string.Equals(x.Path, relativeOldFilePath, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(localOldFile != null, $"File {this.DeletedFile.FullName} does not exists in db");

            var remoteOldFile = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, relativeOldFilePath, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(remoteOldFile != null, $"File {this.DeletedFile.FullName} does not exists in db");

            System.Diagnostics.Debug.Assert(localOldFile.Path == remoteOldFile.Path, "Error #38456923845");

            if (localOldFile.Length != (ulong)this.CreatedFile.Length
                || remoteOldFile.Size != localOldFile.Length
                //// || localOldFile.LastWriteTime != this.CreatedFile.LastWriteTime
                //// || remoteOldFile.UploadTime != localOldFile.LastWriteTimeUnix
                || remoteOldFile.Sha1 != localOldFile.Sha1
                || remoteOldFile.Path != localOldFile.Path)
            {
                throw new ApplicationException();
            }

#if DEBUG
            if (syncObjectCreated != syncObjectDeleted)
            {
#warning cross-object moving isn't implemented
                throw new NotImplementedException();
            }
#endif
            var syncObject = syncObjectCreated;
            int? newFolderUploadId;
            using (var cmd = new CommandEnsureFolderExists(this.SyncDb, syncObjectCreated, Path.GetDirectoryName(this.CreatedFile.FullName)))
            {
                cmd.Execute(connection);
                newFolderUploadId = cmd.Result;
            }

            System.Diagnostics.Debug.Assert(newFolderUploadId.HasValue, $"Server folder {Path.GetDirectoryName(this.CreatedFile.FullName)} had not been created");

            this.SyncDb.LocalModifications.Add(new LocalFileModified(localOldFile.Path) { LocalFileOld = localOldFile });
            this.SyncDb.RemoteModifications.Add(new RemoteFileModified(remoteOldFile.Path) { RemoteFileOld = remoteOldFile });

            var folderContentsBeforeMove = connection.GetChildren(syncObject.Token, newFolderUploadId);
            connection.Move(syncObject.Token, newFolderUploadId.Value, remoteOldFile.UploadId);
            var folderContentsAfterMove = connection.GetChildren(syncObject.Token, newFolderUploadId);

            var addedItem = folderContentsAfterMove.Single(x => !folderContentsBeforeMove.Any(z1 => z1.UploadId == x.UploadId));

            System.Diagnostics.Debug.Assert(remoteOldFile.UploadId != addedItem.UploadId);

            bool newFileExists = connection.Exists(syncObject.Token, newFolderUploadId.Value, Path.GetFileName(this.CreatedFile.FullName));

            var relativeNewFilePath = this.CreatedFile.FullName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            var remoteOldFileFolder = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, Path.GetDirectoryName(relativeOldFilePath), StringComparison.InvariantCultureIgnoreCase));
            var remoteNewFileFolder = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, Path.GetDirectoryName(relativeNewFilePath), StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(remoteNewFileFolder.UploadId == newFolderUploadId.Value, "Error #82374629384");
            bool oldFileExists = connection.Exists(syncObject.Token, remoteOldFileFolder.UploadId, Path.GetFileName(this.CreatedFile.FullName));

            if (!newFileExists || oldFileExists)
            {
                throw new ApplicationException();
            }

            localOldFile.Path = relativeNewFilePath;
            remoteOldFile.Path = localOldFile.Path;
            remoteOldFile.UploadId = addedItem.UploadId;

            remoteOldFile.ParentRemoteFileId = remoteNewFileFolder.RemoteFileId;

            this.SyncDb.LocalModifications.Add(new LocalFileModified(localOldFile.Path) { LocalFileNew = localOldFile });
            this.SyncDb.RemoteModifications.Add(new RemoteFileModified(remoteOldFile.Path) { RemoteFileNew = remoteOldFile });

            this.SyncDb.SaveChanges();
        }
    }
}
