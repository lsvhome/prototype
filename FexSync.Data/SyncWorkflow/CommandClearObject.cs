using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Net.Fex.Api;

namespace FexSync.Data
{
    public class CommandClearObject : CommandBaseAuthorizedUser
    {
        private string Token { get; set; }

        public CommandClearObject(string token) : base(new Dictionary<string, string>())
        {
            this.Token = token;
        }

        protected override string Suffix => throw new NotImplementedException();

        public override void Execute(IConnection connection)
        {
#if DEBUG
            var rootObjects = connection.GetChildren(this.Token, null);
            foreach (var each in rootObjects)
            {
                connection.DeleteFile(this.Token, each.UploadId);
            }

            System.Diagnostics.Debug.Assert(connection.GetChildren(this.Token, null).Length == 0);
#endif
        }
    }
}
