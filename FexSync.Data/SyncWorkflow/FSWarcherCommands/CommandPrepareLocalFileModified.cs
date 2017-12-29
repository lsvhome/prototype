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
        private void Watcher_OnFileModified(object sender, FileModifiedEventArgs e)
        {
            this.Task_RunSafe(() => { this.Watcher_OnFileModified(e.FullPath); });
        }

        private void Watcher_OnFileModified(string fullPath)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            syncDb.LockedRun(() =>
            {
                var fi = new FileInfo(fullPath);

                if (!fi.Exists)
                {
                    throw new ApplicationException();
                }

                using (fi.OpenWrite())
                {
                }

                var syncObject = this.config.SyncObjects.Single(x => fullPath.Contains(x.Path));

                using (var cmd = new CommandPrepareLocalFileModified(syncDb, fi, syncObject.Path))
                {
                    cmd.Execute(this.connection);
                }
            });

            this.Transfer(this.connection);
        }
    }

    public class CommandPrepareLocalFileModified : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private FileInfo ModifiedFile { get; set; }

        private string AccountDataFolder { get; set; }

        public CommandPrepareLocalFileModified(ISyncDataDbContext context, FileInfo modifiedFile, string accountDataFolder) : base(new Dictionary<string, string>())
        {
            System.Diagnostics.Debug.Assert(modifiedFile.Exists, $"File {modifiedFile.FullName} does not exists");
            this.SyncDb = context;
            this.ModifiedFile = modifiedFile;
            this.AccountDataFolder = accountDataFolder;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            var relativeModifiedName = this.ModifiedFile.FullName.Replace(this.AccountDataFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            var localFile = this.SyncDb.LocalFiles.SingleOrDefault(x => string.Equals(x.Path, relativeModifiedName, StringComparison.InvariantCultureIgnoreCase));
            System.Diagnostics.Debug.Assert(localFile != null, $"File {this.ModifiedFile.FullName} does not exists");

            var newSha1 = this.ModifiedFile.Sha1();
            if (localFile.Length != (ulong)this.ModifiedFile.Length || localFile.Sha1 != newSha1)
            {
                var mod = new LocalFileModified
                {
                    LocalFileOld = localFile
                };

                localFile.Length = (ulong)this.ModifiedFile.Length;
                localFile.Sha1 = newSha1;
                localFile.LastWriteTime = this.ModifiedFile.LastWriteTime;

                mod.LocalFileNew = localFile;
                this.SyncDb.LocalModifications.Add(mod);

                if (!this.SyncDb.Uploads.Any(item => item.Path == this.ModifiedFile.FullName))
                {
                    this.SyncDb.Uploads.Add(new UploadItem { Path = localFile.Path });
                }

                this.SyncDb.SaveChanges();
            }
            else
            {
                throw new ApplicationException();
            }
        }
    }
}
