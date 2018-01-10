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
        private void Watcher_OnFileCreated(object sender, FileCreatedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("Watcher_OnFileCreated");
            this.Task_RunSafe(() => { this.Watcher_OnFileCreated(e.FullPath); });
        }

        private void Watcher_OnFileCreated(string fullPath)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            syncDb.LockedRun(() =>
            {
                var fi = new FileInfo(fullPath);

                if (!fi.Exists)
                {
                    throw new ApplicationException();
                }

                //// check file is accessible
                using (fi.OpenWrite())
                {
                }

                var syncObject = this.config.SyncObjects.Single(x => fullPath.Contains(x.Path));

                using (var cmd = new CommandPrepareLocalFileCreated(syncDb, fi, syncObject))
                {
                    cmd.Execute(this.connection);
                }
            });

            this.Transfer(this.connection);
        }
    }

    public class CommandPrepareLocalFileCreated : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private FileInfo NewFile { get; set; }

        private AccountSyncObject SyncObject { get; set; }

        public CommandPrepareLocalFileCreated(ISyncDataDbContext context, FileInfo newFile, AccountSyncObject syncObject) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(newFile.Exists, $"File {newFile.FullName} does not exists");
            this.SyncDb = context;
            this.NewFile = newFile;
            this.SyncObject = syncObject;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            System.Diagnostics.Debug.Assert(!this.SyncDb.LocalFiles.Any(x => string.Equals(x.Path, this.NewFile.FullName, StringComparison.InvariantCultureIgnoreCase)), $"File {this.NewFile.FullName} already indexed");
            var localFilePath = this.NewFile.FullName.Replace(this.SyncObject.Path, string.Empty).Trim(Path.DirectorySeparatorChar);
            var localFile = new LocalFile(localFilePath, this.SyncObject.Token)
            {
                Length = (ulong)this.NewFile.Length,
                LastWriteTime = this.NewFile.LastWriteTime,
                Sha1 = this.NewFile.Sha1()
            };

            this.SyncDb.LocalFiles.Add(localFile);
            this.SyncDb.LocalModifications.Add(new LocalFileModified(localFile.Path) { LocalFileNew = localFile });

            if (!this.SyncDb.Uploads.Any(item => item.Path == localFile.Path))
            {
                this.SyncDb.Uploads.Add(new UploadItem(localFile.Path, this.SyncObject.Token));
            }

            this.SyncDb.SaveChanges();
        }
    }
}
