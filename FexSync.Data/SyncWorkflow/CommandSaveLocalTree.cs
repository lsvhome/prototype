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

        private DirectoryInfo DataFolder { get; set; }

        public CommandSaveLocalTree(ISyncDataDbContext context, DirectoryInfo dataFolder) : base(new Dictionary<string, string>())
        {
            this.SyncDb = context;
            this.DataFolder = dataFolder;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            this.BuildLocalList();
        }

        protected void BuildLocalList()
        {
            var localFiles = this.GetLocalFiles();

            var savedFiles = this.SyncDb.LocalFiles.ToList();

            var removedLocalFiles = savedFiles.Where(item => !localFiles.Keys.Contains(item.Path)).ToList();
            var added = localFiles.Keys.Where(item => !savedFiles.Select(z => z.Path).Contains(item)).ToList();
            var modifiedPaths =
                from oldVersion in savedFiles
                from localFileKey in localFiles.Keys
                where oldVersion.Path == localFileKey
                && (oldVersion.Length != (ulong)localFiles[localFileKey].Length
                ||
                oldVersion.LastWriteTime != localFiles[localFileKey].LastWriteTime)
                select new { oldVersion, localFileKey };

            this.SyncDb.LocalFiles.RemoveRange(removedLocalFiles);
            this.SyncDb.SaveChanges();

            foreach (var each in added)
            {
                var fi = localFiles[each];
                var localFile = new LocalFile
                {
                    Path = fi.FullName.Replace(this.DataFolder.FullName, string.Empty).Trim(Path.DirectorySeparatorChar),
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

                var newVersion = new LocalFile
                {
                    Path = fileInfo.FullName.Replace(this.DataFolder.FullName, string.Empty).Trim(Path.DirectorySeparatorChar),
                    Length = (ulong)fileInfo.Length,
                    LastWriteTime = fileInfo.LastWriteTime,
                    Sha1 = fileInfo.Sha1()
                };

                this.SyncDb.LocalFiles.Add(newVersion);
                this.SyncDb.LocalFiles.Remove(each.oldVersion);

                this.SyncDb.LocalModifications.Add(new LocalFileModified { LocalFileOld = each.oldVersion, LocalFileNew = newVersion, Path = each.oldVersion.Path });

                this.SyncDb.SaveChanges();
            }
        }

        private Dictionary<string, FileInfo> GetLocalFiles()
        {
            var allFiles = System.IO.Directory.GetFiles(this.DataFolder.FullName, "*", SearchOption.AllDirectories);

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
