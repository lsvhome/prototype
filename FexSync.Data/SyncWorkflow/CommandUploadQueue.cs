using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandUploadQueue : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private DirectoryInfo DataFolder { get; set; }

        private string TokenForSync { get; set; }

        public CommandUploadQueue(ISyncDataDbContext context, DirectoryInfo dataFolder, string tokenForSync) : base(new Dictionary<string, string>())
        {
            this.SyncDb = context;
            this.DataFolder = dataFolder;
            this.TokenForSync = tokenForSync;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            this.Upload(connection);
        }

        private void Upload(IConnection conn)
        {
            var maxTriesCount = this.SyncDb.Uploads.Max(x => (int?)x.TriesCount) ?? 0;
            UploadItem ui = null;
            while ((ui = this.SyncDb.Uploads.Where(x => x.TriesCount <= maxTriesCount).OrderBy(x => x.TriesCount).ThenByDescending(x => x.ItemCreated).FirstOrDefault()) != null)
            {
                if (conn.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                ui.TriesCount++;
                this.SyncDb.SaveChanges();
                try
                {
                    var localPath = Path.Combine(this.DataFolder.FullName, ui.Path.Trim(Path.DirectorySeparatorChar));

                    if (File.Exists(localPath))
                    {
                        var localDirectory = Path.GetDirectoryName(localPath);
                        var localDirectoryRelativePath = localDirectory.Replace(this.DataFolder.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                        int? folderUploadId = this.SyncDb.RemoteFiles.SingleOrDefault(item => string.Equals(item.Path, localDirectoryRelativePath, StringComparison.InvariantCultureIgnoreCase))?.UploadId;

                        if (!folderUploadId.HasValue)
                        {
                            using (var cmdEnsureFolderExists = new CommandEnsureFolderExists(this.SyncDb, this.DataFolder, this.TokenForSync, localDirectory))
                            {
                                cmdEnsureFolderExists.Execute(conn);
                                folderUploadId = cmdEnsureFolderExists.Result;
                            }
                        }
                        
                        var x11 = this.SyncDb.RemoteFiles.SingleOrDefault(item => string.Equals(item.Path, localDirectoryRelativePath, StringComparison.InvariantCultureIgnoreCase));
                        System.Diagnostics.Debug.Assert(folderUploadId == x11?.UploadId);

                        var folderContentsBeforeUpload = conn.GetChildren(this.TokenForSync, folderUploadId).Where(x => string.Equals(x.Name, Path.GetFileName(localPath), StringComparison.InvariantCultureIgnoreCase)).ToList();

#if DEBUG
                        var parentFolder = this.SyncDb.RemoteFiles.SingleOrDefault(item => string.Equals(item.Path, localDirectoryRelativePath, StringComparison.InvariantCultureIgnoreCase));
                        if (parentFolder == null && localDirectoryRelativePath != this.TokenForSync)
                        {
                            throw new ApplicationException();
                        }
#endif

                        var uploadedFile = conn.Upload(this.TokenForSync, folderUploadId, localPath);
                        if (folderContentsBeforeUpload.Any())
                        {
                            var trashFolder = conn.ObjectView(this.TokenForSync).UploadList.Single(x => string.Equals(x.Name, AccountSettings.TrashBinFolderName, StringComparison.InvariantCultureIgnoreCase));
                            conn.Move(this.TokenForSync, trashFolder.UploadId, folderContentsBeforeUpload.Select(item => item.UploadId).ToArray());
                        }

#if DEBUG
                        var folderContentsAfterUpload = conn.GetChildren(this.TokenForSync, folderUploadId).Where(item => string.Equals(item.Name, Path.GetFileName(localPath), StringComparison.InvariantCultureIgnoreCase)).ToList();
                        if (folderContentsAfterUpload.Count() != 1)
                        {
                            throw new ApplicationException();
                        }
#endif

                        var relativePath = ui.Path.Replace(this.DataFolder.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                        var removeFileQuery = this.SyncDb.RemoteFiles.Where(item => string.Equals(item.Path, relativePath, StringComparison.InvariantCultureIgnoreCase));

                        System.Diagnostics.Debug.Assert(removeFileQuery.Count() <= 1, $"{removeFileQuery.Count()} files with identical paths found: {ui.Path}");

                        var remoteFile = removeFileQuery.SingleOrDefault();
                        if (remoteFile == null)
                        {
                            remoteFile = new RemoteFile();
                            remoteFile.RemoteTreeId = this.SyncDb.RemoteTrees.OrderBy(item => item.Created).Last().RemoteTreeId;
                            this.SyncDb.RemoteFiles.Add(remoteFile);
                        }

                        remoteFile.Path = ui.Path;
                        remoteFile.Name = uploadedFile.Name;
                        remoteFile.Sha1 = uploadedFile.Sha1;
                        remoteFile.Crc32 = uploadedFile.Crc32;
                        remoteFile.Size = uploadedFile.Size;
                        remoteFile.UploadId = uploadedFile.UploadId;
                        remoteFile.UploadTime = uploadedFile.UploadTime;

                        remoteFile.Token = this.TokenForSync;
                        remoteFile.Crc32 = uploadedFile.Crc32;

                        var remoteFileUnixTime = remoteFile.UploadTime.FromUnixTime();
                        File.SetCreationTime(localPath, remoteFileUnixTime);
                        File.SetLastWriteTime(localPath, remoteFileUnixTime);

                        this.SyncDb.Uploads.Remove(ui);
                        this.SyncDb.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    ex.Process();
                    this.SyncDb.AcceptAllChangesWithoutSaving();
                    throw;
                }
            }
        }
    }
}
