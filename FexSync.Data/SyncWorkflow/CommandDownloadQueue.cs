using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandDownloadQueue : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private DirectoryInfo DataFolder { get; set; }

        public CommandDownloadQueue(ISyncDataDbContext context, DirectoryInfo dataFolder) : base(new Dictionary<string, string>())
        {
            this.SyncDb = context;
            this.DataFolder = dataFolder;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            this.Download(connection);
        }

        private void Download(IConnection conn)
        {
            var maxTriesCount = this.SyncDb.Downloads.Max(x => (int?)x.TriesCount) ?? 0;
            DownloadItem di = null;
            while ((di = this.SyncDb.Downloads.Where(item => item.TriesCount <= maxTriesCount).OrderBy(item => item.TriesCount).ThenByDescending(x => x.ItemCreated).FirstOrDefault()) != null)
            {
                conn.CancellationToken.ThrowIfCancellationRequested();

                di.TriesCount++;
                this.SyncDb.SaveChanges();
                try
                {
                    var downloadPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    var localPath = Path.Combine(this.DataFolder.FullName, di.FilePathLocalRelative);
                    conn.Get(di.Token, di.UploadId, downloadPath);
                    if (File.Exists(downloadPath))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(localPath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                        }

                        if (File.Exists(localPath))
                        {
                            System.IO.File.Replace(downloadPath, localPath, localPath + ".bak");
                        }
                        else
                        {
                            System.IO.File.Move(downloadPath, localPath);
                        }
                    }

                    var remoteFile = this.SyncDb.RemoteFiles.Single(x => x.Path == di.FilePathLocalRelative);

                    var dt = remoteFile.UploadTime.FromUnixTime();
                    File.SetCreationTime(localPath, dt);
                    File.SetLastWriteTime(localPath, dt);

                    var localFile = new LocalFile
                    {
                        Path = remoteFile.Path,
                        Length = remoteFile.Size,
                        LastWriteTime = dt,
                        Sha1 = remoteFile.Sha1
                    };

                    this.SyncDb.LocalFiles.Add(localFile);
                    this.SyncDb.Downloads.Remove(di);
                    this.SyncDb.SaveChanges();
                }
                catch (Exception)
                {
                    this.SyncDb.AcceptAllChangesWithoutSaving();
                }
            }
        }
    }
}
