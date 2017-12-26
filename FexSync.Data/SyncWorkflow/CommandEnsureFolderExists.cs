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

        private DirectoryInfo dataFolder;

        private string folderPath;

        private string token;

        public int? Result { get; private set; } = null;

        public CommandEnsureFolderExists(ISyncDataDbContext context, DirectoryInfo dataFolder, string token, string folderPath) : base(new Dictionary<string, string>())
        {
            this.dataFolder = dataFolder;
            this.folderPath = folderPath;
            this.token = token;
            this.SyncDb = context;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            var relativePath = this.folderPath.Replace(this.dataFolder.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar);

            this.Result = this.ExecuteInternal(connection, relativePath);
        }

        private int? ExecuteInternal(IConnection connection, string relativePath)
        {
            var remoteFile = this.SyncDb.RemoteFiles.SingleOrDefault(item => item.Path == relativePath);

            if (remoteFile != null)
            {
                return remoteFile.UploadId;
            }

            var pathItems = relativePath.Split(new[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);

            var name = Path.GetFileName(relativePath);
            if (pathItems.Length < 1)
            {
                throw new ApplicationException();
            }
            else if (pathItems.Length == 1)
            {
                return null;
            }
            else if (pathItems.Length == 2)
            {
                try
                {
                    var uploadId = connection.CreateFolder(this.token, null, name);
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

                var uploadId = connection.CreateFolder(this.token, parentId, name);
                this.AddFolderToDb(relativePath, uploadId);
                return uploadId;
            }
        }

        private void AddFolderToDb(string relativePath, int id)
        {
            var remoteTreeId = this.SyncDb.RemoteTrees.OrderBy(item => item.Created).Last().RemoteTreeId;

            var remoteFolder = new RemoteFile
            {
                RemoteTreeId = remoteTreeId,
                Token = this.token,
                UploadId = id,
                Path = relativePath,
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
