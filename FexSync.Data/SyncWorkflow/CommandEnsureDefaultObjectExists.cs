using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandEnsureDefaultObjectExists : CommandBaseAuthorizedUser
    {
        private string DefaultFexSyncFolderName { get; set; }

        public CommandArchive.CommandArchiveResponseObject Result { get; private set; } = null;

        public CommandEnsureDefaultObjectExists(string defaultFexSyncFolderName) : base(new Dictionary<string, string>())
        {
            this.DefaultFexSyncFolderName = defaultFexSyncFolderName;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
            var conn = connection;
            var defaultObject = conn.Archive(0, 1000).ObjectList.FirstOrDefault(x => x.Preview == this.DefaultFexSyncFolderName);
            if (defaultObject == null)
            {
                var newObject = conn.CreateObject();
                System.Diagnostics.Debug.Assert(newObject.CanEdit == 1, "CanEdit == 0");
                conn.ObjectUpdate(newObject.Token, this.DefaultFexSyncFolderName);
                defaultObject = conn.Archive(0, 1000).ObjectList.FirstOrDefault(obj => obj.Preview == this.DefaultFexSyncFolderName);
                if (defaultObject == null)
                {
                    throw new ApplicationException();
                }
            }

            this.Result = defaultObject;
        }
    }
}
