using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandSaveRemoteTree : Net.Fex.Api.CommandBaseAuthorizedUser
    {
        private ISyncDataDbContext SyncDb { get; set; }

        private string TokenForSync { get; set; }

        public int? Result { get; private set; } = null;

        public CommandSaveRemoteTree(ISyncDataDbContext context, string tokenForSync) : base(new Dictionary<string, string>())
        {
            this.SyncDb = context;
            this.TokenForSync = tokenForSync;
        }

        public override void Execute(IConnection connection)
        {
            this.Result = this.BuildRemoteList(connection);
        }

        protected override string Suffix => throw new NotImplementedException();

        protected int BuildRemoteList(IConnection conn)
        {
            CommandBuildRemoteTree.CommandBuildRemoteTreeResponse tree;

            using (var cmd = new CommandBuildRemoteTree { Token = this.TokenForSync })
            {
                cmd.Execute(conn);
                tree = cmd.Result;
            }

            System.Diagnostics.Debug.Assert(tree.List.All(listItem => listItem is CommandBuildRemoteTree.CommandBuildRemoteTreeItemArchive), "Type check failed");

            var root = tree.List.OfType<CommandBuildRemoteTree.CommandBuildRemoteTreeItemArchive>().FirstOrDefault(item => item.ArchiveObject.Token == this.TokenForSync);

            var treeItem = new RemoteTree { Created = DateTime.Now };
            this.SyncDb.RemoteTrees.Add(treeItem);
            this.SyncDb.SaveChanges();

            if (root != null)
            {
                // hide trash contents
                var trashFolder = root.Childern.SingleOrDefault(x => x.Object.Name == AccountSettings.TrashBinFolderName);
                if (trashFolder != null)
                {
                    root.Childern.Remove(trashFolder);
                }

                foreach (var each in root.Childern.FilterUniqueNames())
                {
                    this.SaveRemoteItemRecursive(each, null, this.SyncDb, treeItem.RemoteTreeId);
                }

                this.SyncDb.SaveChanges();

                // find modified files
                var modifications =
                    from fl in this.SyncDb.RemoteFiles
                    from fp in this.SyncDb.RemoteFiles
                    where fl.Path == fp.Path
                    && fl.Sha1 != fp.Sha1
                    && fl.RemoteTreeId > fp.RemoteTreeId
                    select new { fl, fp };

                foreach (var each in modifications)
                {
                    var modifiedItem = new RemoteFileModified();
                    modifiedItem.RemoteFileOld = each.fp;
                    modifiedItem.RemoteFileNew = each.fl;
                    this.SyncDb.RemoteModifications.Add(modifiedItem);
                    this.SyncDb.SaveChanges();
                }
            }

            // clear old metadata
            this.SyncDb.RemoteFiles.RemoveRange(this.SyncDb.RemoteFiles.Where(x => x.RemoteTreeId != treeItem.RemoteTreeId));
            this.SyncDb.SaveChanges();
#if DEBUG       
            if (this.SyncDb.RemoteFiles.Select(x => x.RemoteTreeId).Distinct().Count() > 1)
            {
                throw new ApplicationException();
            }
#endif
            return treeItem.RemoteTreeId;
        }

        private void SaveRemoteItemRecursive(CommandBuildRemoteTree.CommandBuildRemoteTreeItemObject item, int? parentId, ISyncDataDbContext syncDb, int remoteTreeId)
        {
            var remoteFile = syncDb.RemoteFiles.SingleOrDefault(x => x.Token == item.Token && x.UploadId == item.UploadId && x.RemoteTreeId == remoteTreeId);

            if (remoteFile == null || !this.IsItemsEqual(item, remoteFile))
            {
                remoteFile = new RemoteFile
                {
                    RemoteTreeId = remoteTreeId,
                    Token = item.Token,
                    UploadId = item.UploadId,
                    Path = item.Path,
                    Name = item.Object.Name,
                    UploadTime = item.Object.UploadTime,
                    Size = item.Object.Size,
                    Sha1 = item.Object.Sha1
                };

                syncDb.RemoteFiles.Add(remoteFile);
            }

            if (item.Object.IsFolder == 1)
            {
                foreach (var each in item.Childern.FilterUniqueNames())
                {
                    this.SaveRemoteItemRecursive(each, remoteFile.RemoteFileId, syncDb, remoteTreeId);
                }
            }
        }

        private bool IsItemsEqual(CommandBuildRemoteTree.CommandBuildRemoteTreeItemObject item, RemoteFile remoteFile)
        {
            if (item == null)
            {
                return false;
            }

            if (remoteFile == null)
            {
                return false;
            }

            if (item.Object.Name != remoteFile.Name)
            {
                return false;
            }

            if (item.Token != remoteFile.Token)
            {
                return false;
            }

            if (item.UploadId != remoteFile.UploadId)
            {
                return false;
            }

            if (item.Object.Size != remoteFile.Size)
            {
                return false;
            }

            if (item.Object.Sha1 != remoteFile.Sha1)
            {
                return false;
            }

            return true;
        }
    }
}
