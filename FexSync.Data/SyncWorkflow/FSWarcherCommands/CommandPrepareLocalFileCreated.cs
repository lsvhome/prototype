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

                using (var cmd = new CommandPrepareLocalFileCreated(syncDb, fi, this.config.AccountSettings.AccountDataFolder))
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

        private string AccountDataFolder { get; set; }

        public CommandPrepareLocalFileCreated(ISyncDataDbContext context, FileInfo newFile, string accountDataFolder) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(newFile.Exists, $"File {newFile.FullName} does not exists");
            this.SyncDb = context;
            this.NewFile = newFile;
            this.AccountDataFolder = accountDataFolder;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            System.Diagnostics.Debug.Assert(!this.SyncDb.LocalFiles.Any(x => string.Equals(x.Path, this.NewFile.FullName, StringComparison.InvariantCultureIgnoreCase)), $"File {this.NewFile.FullName} already indexed");

            var localFile = new LocalFile
            {
                Path = this.NewFile.FullName.Replace(this.AccountDataFolder, string.Empty).Trim(Path.DirectorySeparatorChar),
                Length = (ulong)this.NewFile.Length,
                LastWriteTime = this.NewFile.LastWriteTime,
                Sha1 = this.NewFile.Sha1()
            };

            this.SyncDb.LocalFiles.Add(localFile);
            this.SyncDb.LocalModifications.Add(new LocalFileModified { LocalFileNew = localFile });

            if (!this.SyncDb.Uploads.Any(item => item.Path == localFile.Path))
            {
                this.SyncDb.Uploads.Add(new UploadItem { Path = localFile.Path });
            }

            this.SyncDb.SaveChanges();
        }
    }
}
