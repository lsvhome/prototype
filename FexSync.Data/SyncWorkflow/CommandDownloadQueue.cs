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

        public event EventHandler<FilePathEventArgs> OnBeforeSave;

        public event EventHandler<FilePathEventArgs> OnAfterSave;

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
                            this.OnBeforeSave?.Invoke(this, new FilePathEventArgs { FullPath = Path.GetDirectoryName(localPath) });
                            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                            this.OnAfterSave?.Invoke(this, new FilePathEventArgs { FullPath = Path.GetDirectoryName(localPath) });
                        }

                        System.Diagnostics.Trace.WriteLine($"Downloaded {localPath}");
                        if (File.Exists(localPath))
                        {
                            var trashCopy = Path.Combine(this.DataFolder.FullName, AccountSettings.TrashBinFolderName, Path.GetFileName(localPath));
                            int i = 0;
                            while (File.Exists(trashCopy))
                            {
                                i++;
                                trashCopy = Path.Combine(this.DataFolder.FullName, AccountSettings.TrashBinFolderName, $"copy({i})_" + Path.GetFileName(localPath));
                            }

                            this.OnBeforeSave?.Invoke(this, new FilePathEventArgs { FullPath = localPath });
                            this.OnBeforeSave?.Invoke(this, new FilePathEventArgs { FullPath = trashCopy });
                            System.IO.File.Replace(downloadPath, localPath, trashCopy);
                            this.OnAfterSave?.Invoke(this, new FilePathEventArgs { FullPath = localPath });
                            this.OnAfterSave?.Invoke(this, new FilePathEventArgs { FullPath = trashCopy });
                        }
                        else
                        {
                            this.OnBeforeSave?.Invoke(this, new FilePathEventArgs { FullPath = localPath });
                            System.IO.File.Move(downloadPath, localPath);
                            this.OnAfterSave?.Invoke(this, new FilePathEventArgs { FullPath = localPath });
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
                catch (Exception ex)
                {
                    ex.Process();
                    this.SyncDb.AcceptAllChangesWithoutSaving();
                }
            }
        }
    }
}
