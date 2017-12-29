using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandPrepareDownload : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private int TreeId { get; set; }

        public CommandPrepareDownload(ISyncDataDbContext context, int treeId) : base(new Dictionary<string, string>())
        {
            this.SyncDb = context;
            this.TreeId = treeId;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            this.BuildDownloadList(this.TreeId);
        }

        private void BuildDownloadList(int treeId)
        {
            var remoteFiles = this.SyncDb.RemoteFiles.Where(item => item.RemoteTreeId == treeId);
            var localFiles = this.SyncDb.LocalFiles;

            //// unexisting locally files
            var downloadList = remoteFiles.Where(x => !localFiles.Any(item => item.Path == x.Path)).ToList();

            //// modified files
            var differntFiles =
                from remote in remoteFiles
                from local in localFiles
                where remote.Path == local.Path
                && (remote.Size != local.Length || remote.Sha1 != local.Sha1)
                //// !!!!!!!!!!!!
                && remote.UploadTime > local.LastWriteTimeUnix
                select new { remote, local };

            foreach (var each in differntFiles)
            {
                var remoteModifications = this.SyncDb.RemoteModifications.Where(item => item.Path == each.remote.Path).ToList();
                var localModifications = this.SyncDb.LocalModifications.Where(item => item.Path == each.local.Path).ToList();

                if (remoteModifications.Any() == localModifications.Any())
                {
                    var conflict = new ConflictItem
                    {
                        Path = remoteModifications.First().Path,
                        LocalModifications = localModifications.ToArray(),
                        RemoteModifications = remoteModifications.ToArray()
                    };

                    this.SyncDb.Conflicts.Add(conflict);
                    this.SyncDb.SaveChanges();
                }
                else if (remoteModifications.Any() && !localModifications.Any())
                {
                    downloadList.AddRange(differntFiles.Select(item => item.remote));
                }
                else if (!remoteModifications.Any() && localModifications.Any())
                {
                    //// should be handled with CommandPrepareUpload
                }
                else
                {
                    throw new ApplicationException();
                }
            }

            downloadList.AddRange(differntFiles.Select(x => x.remote));

            downloadList.ForEach(
                (each) =>
                {
                    if (!SyncDb.Downloads.Any(item => item.SyncObject.Token == each.SyncObject.Token && item.UploadId == each.UploadId))
                    {
                        SyncDb.Downloads.Add(new DownloadItem
                        {
                            TriesCount = 0,
                            SyncObject = each.SyncObject,
                            UploadId = each.UploadId,
                            FilePathLocalRelative = each.Path,
                            ItemCreated = DateTime.Now
                        });
                    }
                });

            this.SyncDb.SaveChanges();
        }
    }
}
