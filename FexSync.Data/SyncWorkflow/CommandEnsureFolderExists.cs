using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandEnsureFolderExists : CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private string folderPath;

        private AccountSyncObject SyncObject { get; set; }

        public int? Result { get; private set; } = null;

        public CommandEnsureFolderExists(ISyncDataDbContext context, AccountSyncObject syncObject, string folderPath) : base(new Dictionary<string, string>())
        {
            this.folderPath = folderPath;
            this.SyncDb = context;
            this.SyncObject = syncObject;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            var relativePath = this.folderPath.Replace(this.SyncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

            this.Result = this.ExecuteInternal(connection, relativePath);
        }

        private int? ExecuteInternal(IConnection connection, string relativePath)
        {
            var remoteFile = this.SyncDb.RemoteFiles.SingleOrDefault(item => item.SyncObject.Token == this.SyncObject.Token && item.Path == relativePath);

            if (remoteFile != null)
            {
                return remoteFile.UploadId;
            }

            var pathItems = relativePath.Split(new[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);

            var name = Path.GetFileName(relativePath);
            if (pathItems.Length == 0)
            {
                return null;
            }
            else if (pathItems.Length == 1)
            {
                try
                {
                    var uploadId = connection.CreateFolder(this.SyncObject.Token, null, name);
                    this.AddFolderToDb(relativePath, uploadId);
                    return uploadId;
                }
                catch (Exception ex)
                {
                    ex.Process();
                    throw;
                }
            }
            else
            {
                var parentId = this.ExecuteInternal(connection, Path.GetDirectoryName(relativePath));

                var uploadId = connection.CreateFolder(this.SyncObject.Token, parentId, name);
                this.AddFolderToDb(relativePath, uploadId);
                return uploadId;
            }
        }

        private void AddFolderToDb(string relativePath, int id)
        {
            var remoteTreeId = this.SyncDb.RemoteTrees.OrderBy(item => item.Created).Last().RemoteTreeId;

            var remoteFolder = new RemoteFile(relativePath, remoteTreeId, id, this.SyncObject)
            {
                Name = Path.GetFileName(relativePath),
                UploadTime = DateTime.Now.ToUnixTime(),
                Size = 0,
                Sha1 = string.Empty
            };

            this.SyncDb.RemoteFiles.Add(remoteFolder);
            this.SyncDb.SaveChanges();
        }
    }
}
