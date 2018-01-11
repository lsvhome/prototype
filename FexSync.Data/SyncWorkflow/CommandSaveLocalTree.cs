using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandSaveLocalTree : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private AccountSyncObject SyncObject { get; set; }

        public CommandSaveLocalTree(ISyncDataDbContext context, AccountSyncObject syncObject) : base(new Dictionary<string, string>())
        {
            this.SyncDb = context;
            this.SyncObject = syncObject;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            this.BuildLocalList();
        }

        protected void BuildLocalList()
        {
            //// each iten has absolute path
            var localFiles = this.GetLocalFiles();

            //// LocalFiles are with relative paths, so we have to add fullPath field
            var savedFiles = this.SyncDb.LocalFiles.Where(item => item.Token == this.SyncObject.Token).Select(x => new { file = x, fullPath = Path.Combine(this.SyncObject.Path, x.Path) }).ToList();

            var removedLocalFiles = savedFiles.Where(item => !localFiles.Keys.Contains(item.fullPath)).Select(x => x.file).ToList();
            var added = localFiles.Keys.Where(item => !savedFiles.Select(z => z.fullPath).Contains(item)).ToList();
            var modifiedPaths =
                from oldVersion in savedFiles
                from localFileKey in localFiles.Keys
                where oldVersion.fullPath == localFileKey
                && (oldVersion.file.Length != (ulong)localFiles[localFileKey].Length
                ||
                oldVersion.file.LastWriteTime != localFiles[localFileKey].LastWriteTime)
                select new { oldVersion, localFileKey };

            this.SyncDb.LocalFiles.RemoveRange(removedLocalFiles);
            this.SyncDb.SaveChanges();

            foreach (var each in added)
            {
                var fi = localFiles[each];
                var localFilePath = fi.FullName.Replace(this.SyncObject.Path, string.Empty).Trim(Path.DirectorySeparatorChar);
                var localFile = new LocalFile(localFilePath, this.SyncObject.Token)
                {
                    Length = (ulong)fi.Length,
                    LastWriteTime = fi.LastWriteTime,
                    Sha1 = fi.Sha1()
                };

                this.SyncDb.LocalFiles.Add(localFile);
            }

            this.SyncDb.SaveChanges();

            foreach (var each in modifiedPaths.ToList())
            {
                var fileInfo = localFiles[each.localFileKey];

                var newVersionPath = fileInfo.FullName.Replace(this.SyncObject.Path, string.Empty).Trim(Path.DirectorySeparatorChar);
                var newVersion = new LocalFile(newVersionPath, this.SyncObject.Token)
                {
                    Length = (ulong)fileInfo.Length,
                    LastWriteTime = fileInfo.LastWriteTime,
                    Sha1 = fileInfo.Sha1()
                };

                this.SyncDb.LocalFiles.Add(newVersion);
                this.SyncDb.LocalFiles.Remove(each.oldVersion.file);

                this.SyncDb.LocalModifications.Add(new LocalFileModified(each.oldVersion.file.Path) { LocalFileOld = each.oldVersion.file, LocalFileNew = newVersion });
            }

            this.SyncDb.SaveChanges();
        }

        private Dictionary<string, FileInfo> GetLocalFiles()
        {
            var allFiles = System.IO.Directory.GetFiles(this.SyncObject.Path, "*", SearchOption.AllDirectories);

            Dictionary<string, FileInfo> ret = new Dictionary<string, FileInfo>();

            foreach (var each in allFiles.Where(fn => !fn.EndsWith(".bak", StringComparison.InvariantCultureIgnoreCase)))
            {
                FileInfo fi = new FileInfo(each);
                ret.Add(each, fi);
            }

            return ret;
        }
    }
}
