using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandPrepareUpload : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private int TreeId { get; set; }

        public CommandPrepareUpload(ISyncDataDbContext context, int treeId) : base(new Dictionary<string, string>())
        {
            this.SyncDb = context;
            this.TreeId = treeId;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            this.BuildUploadList(this.TreeId);
        }

        private void BuildUploadList(int treeId)
        {
            var remoteFiles = this.SyncDb.RemoteFiles.Where(item => item.RemoteTreeId == treeId);
            var localFiles = this.SyncDb.LocalFiles;

            //// unexisted remptely files
            var uploadList = localFiles.Where(x => !remoteFiles.Any(item => item.Path == x.Path)).ToList();

            //// modified files
            var differntFiles = 
                from remote in remoteFiles
                from local in localFiles
                where remote.Path == local.Path
                && (remote.Size != local.Length || remote.Sha1 != local.Sha1)
                //// !!!!!!!!!!!!
                && remote.UploadTime < local.LastWriteTime.ToUnixTime()
                select new { remote, local };

            foreach (var each in differntFiles)
            {
                var remoteModifications = this.SyncDb.RemoteModifications.Where(item => item.Path == each.remote.Path).ToList();
                var localModifications = this.SyncDb.LocalModifications.Where(item => item.Path == each.local.Path).ToList();

                if (remoteModifications.Any() == localModifications.Any())
                {
                    var conflict = new ConflictItem
                    {
                        Path = each.local.Path,
                        LocalModifications = localModifications.ToArray(),
                        RemoteModifications = remoteModifications.ToArray()
                    };

                    this.SyncDb.Conflicts.Add(conflict);
                    this.SyncDb.SaveChanges();
                }
                else if (remoteModifications.Any() && !localModifications.Any())
                {
                    //// should be handled with CommandPrepareDownload
                }
                else if (!remoteModifications.Any() && localModifications.Any())
                {
                    uploadList.AddRange(differntFiles.Select(item => item.local));
                }
                else
                {
                    throw new ApplicationException();
                }
            }

            uploadList.ForEach(
                (each) =>
                {
                    if (!this.SyncDb.Uploads.Any(item => item.Path == each.Path))
                    {
                        this.SyncDb.Uploads.Add(new UploadItem { Path = each.Path });
                    }
                });

            this.SyncDb.SaveChanges();
        }
    }
}
