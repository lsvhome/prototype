using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Fex.Api
{
    public class SyncData
    {
        public CommandBuildRemoteTree.CommandBuildRemoteTreeResponse Remote { get; set; }

        public CommandBuildLocalTree.CommandBuildLocalTreeResponse Local { get; set; }

        public IList<CommandBuildLocalTree.LocalFileMetaData> UploadList { get; set; } = new List<CommandBuildLocalTree.LocalFileMetaData>();

        public IList<CommandObjectView.CommandObjectViewResponseObject> DownloadList { get; set; } = new List<CommandObjectView.CommandObjectViewResponseObject>();
    }
}
