using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandBuildRemoteTree : CommandBaseAuthorizedUser
    {
        public class CommandBuildRemoteTreeResponse
        {
            public CommandBuildRemoteTreeResponse() : this(new CommandBuildRemoteTreeItem[0])
            {
            }

            public CommandBuildRemoteTreeResponse(params CommandBuildRemoteTreeItem[] list)
            {
                this.List = list;
            }

            public CommandBuildRemoteTreeItem[] List { get; private set; }
        }

        public abstract class CommandBuildRemoteTreeItem
        {
            public CommandBuildRemoteTreeItem Parent { get; set; }

            public string Token { get; set; }

            public List<CommandBuildRemoteTreeItemObject> Childern { get; private set; } = new List<CommandBuildRemoteTreeItemObject>();
        }

        public class CommandBuildRemoteTreeItemArchive : CommandBuildRemoteTreeItem
        {
            public CommandArchive.CommandArchiveResponseObject ArchiveObject { get; set; }
        }

        public class CommandBuildRemoteTreeItemObject : CommandBuildRemoteTreeItem
        {
            public CommandObjectView.CommandObjectViewResponseObject Object { get; set; }

            public int UploadId { get; set; }

            public string Path
            {
                get
                {
                    if (this.Parent is CommandBuildRemoteTreeItemObject p)
                    {
                        return p.Path + System.IO.Path.DirectorySeparatorChar + this.Object.Name;
                    }
                    else if (this.Parent is CommandBuildRemoteTreeItemArchive pa)
                    {
                        return this.Object.Name;
                    }
                    else
                    {
                        throw new ApplicationException();
                    }
                }
            }
        }

        public CommandBuildRemoteTree() : base(new Dictionary<string, string>())
        {
        }

        protected override string Suffix => string.Empty;

        public string Token { get; set; } = string.Empty;

        public override void Execute(IConnection connection)
        {
            List<CommandBuildRemoteTreeItem> result = new List<CommandBuildRemoteTreeItem>();
            List<CommandArchive.CommandArchiveResponseObject> rootObjects = new List<CommandArchive.CommandArchiveResponseObject>();
            CommandArchive.CommandArchiveResponse archiveResponse;

            int offset = 0;
            do
            {
                connection.CancellationToken.ThrowIfCancellationRequested();
                archiveResponse = connection.Archive(offset, 100);
                offset += archiveResponse.Count;
                rootObjects.AddRange(archiveResponse.ObjectList);
            }
            while (archiveResponse.Count == archiveResponse.Limit);

            foreach (var each in rootObjects.Where(obj => string.IsNullOrWhiteSpace(this.Token) || string.Equals(this.Token, obj.Token)))
            {
                connection.CancellationToken.ThrowIfCancellationRequested();
                this.ExecuteObject(connection, each, result);
            }

            this.Result = new CommandBuildRemoteTreeResponse(result.ToArray());
        }

        public void ExecuteObject(IConnection connection, CommandArchive.CommandArchiveResponseObject objectArchive, IList<CommandBuildRemoteTreeItem> list)
        {
            var props = connection.ObjectView(objectArchive.Token);
            var item = new CommandBuildRemoteTreeItemArchive { ArchiveObject = objectArchive, Parent = null, Token = objectArchive.Token };
            list.Add(item);
            foreach (var each in props.UploadList)
            {
                connection.CancellationToken.ThrowIfCancellationRequested();

                if (each.IsFolder == 1)
                {
                    this.ExecuteFolder(connection, objectArchive.Token, each, item);
                }
                else
                {
                    this.ExecuteFile(connection, objectArchive.Token, each, item);
                }
            }
        }

        public void ExecuteFolder(IConnection connection, string token, CommandObjectView.CommandObjectViewResponseObject objectView, CommandBuildRemoteTreeItem parent)
        {
            var item = new CommandBuildRemoteTreeItemObject { Object = objectView, Parent = parent, Token = token, UploadId = objectView.UploadId };
            parent.Childern.Add(item);
            var props = connection.ObjectFolderView(token, objectView.UploadId);
            foreach (var each in props.UploadList)
            {
                connection.CancellationToken.ThrowIfCancellationRequested();
                if (each.IsFolder == 1)
                {
                    this.ExecuteFolder(connection, token, each, item);
                }
                else
                {
                    this.ExecuteFile(connection, token, each, item);
                }
            }
        }

        public void ExecuteFile(IConnection connection, string token, CommandObjectView.CommandObjectViewResponseObject x, CommandBuildRemoteTreeItem parent)
        {
            var item = new CommandBuildRemoteTreeItemObject { Object = x, Parent = parent, Token = token, UploadId = x.UploadId };
            parent.Childern.Add(item);
        }

        public CommandBuildRemoteTreeResponse Result { get; private set; }
    }
}
