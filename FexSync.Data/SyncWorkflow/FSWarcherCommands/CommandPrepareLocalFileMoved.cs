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

                    using (var cmd = new CommandPrepareLocalFileMoved(syncDb, fiOld, fiNew, this.config.AccountSettings.AccountDataFolder))
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

        private string AccountDataFolder { get; set; }

        public CommandPrepareLocalFileMoved(ISyncDataDbContext context, FileInfo deletedFile, FileInfo createdFile, string accountDataFolder) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(createdFile.Exists, $"File {createdFile.FullName} does not exists");
            this.SyncDb = context;
            this.DeletedFile = deletedFile;
            this.CreatedFile = createdFile;
            this.AccountDataFolder = accountDataFolder;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            var relativeOldFilePath = this.DeletedFile.FullName.Replace(this.AccountDataFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar);
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

            var oldObjectToken = localOldFile.Path.Split(Path.DirectorySeparatorChar).First();
            var newObjectToken = this.CreatedFile.FullName.Replace(this.AccountDataFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar).First();
#if DEBUG
            if (oldObjectToken != newObjectToken)
            {
#warning cross-object moving isn't implemented
                throw new NotImplementedException();
            }
#endif
            var objectToken = newObjectToken;

            int? newFolderUploadId;
            using (var cmd = new CommandEnsureFolderExists(this.SyncDb, new DirectoryInfo(this.AccountDataFolder), objectToken, Path.GetDirectoryName(this.CreatedFile.FullName)))
            {
                cmd.Execute(connection);
                newFolderUploadId = cmd.Result;
            }

            System.Diagnostics.Debug.Assert(newFolderUploadId.HasValue, $"Server folder {Path.GetDirectoryName(this.CreatedFile.FullName)} had not been created");

            this.SyncDb.LocalModifications.Add(new LocalFileModified { Path = localOldFile.Path, LocalFileOld = localOldFile });
            this.SyncDb.RemoteModifications.Add(new RemoteFileModified { Path = remoteOldFile.Path, RemoteFileOld = remoteOldFile });

            var folderContentsBeforeMove = connection.GetChildren(objectToken, newFolderUploadId);
            connection.Move(objectToken, newFolderUploadId.Value, remoteOldFile.UploadId);
            var folderContentsAfterMove = connection.GetChildren(objectToken, newFolderUploadId);

            var addedItem = folderContentsAfterMove.Single(x => !folderContentsBeforeMove.Any(z1 => z1.UploadId == x.UploadId));

            System.Diagnostics.Debug.Assert(remoteOldFile.UploadId != addedItem.UploadId);

            bool newFileExists = connection.Exists(objectToken, newFolderUploadId.Value, Path.GetFileName(this.CreatedFile.FullName));

            var relativeNewFilePath = this.CreatedFile.FullName.Replace(this.AccountDataFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            var remoteOldFileFolder = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, Path.GetDirectoryName(relativeOldFilePath), StringComparison.InvariantCultureIgnoreCase));
            var remoteNewFileFolder = this.SyncDb.RemoteFiles.SingleOrDefault(x => string.Equals(x.Path, Path.GetDirectoryName(relativeNewFilePath), StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(remoteNewFileFolder.UploadId == newFolderUploadId.Value, "Error #82374629384");
            bool oldFileExists = connection.Exists(objectToken, remoteOldFileFolder.UploadId, Path.GetFileName(this.CreatedFile.FullName));

            if (!newFileExists || oldFileExists)
            {
                throw new ApplicationException();
            }

            localOldFile.Path = relativeNewFilePath;
            remoteOldFile.Path = localOldFile.Path;
            remoteOldFile.UploadId = addedItem.UploadId;

            remoteOldFile.ParentRemoteFileId = remoteNewFileFolder.RemoteFileId;

            this.SyncDb.LocalModifications.Add(new LocalFileModified { Path = localOldFile.Path, LocalFileNew = localOldFile });
            this.SyncDb.RemoteModifications.Add(new RemoteFileModified { Path = remoteOldFile.Path, RemoteFileNew = remoteOldFile });

            this.SyncDb.SaveChanges();
        }
    }
}
