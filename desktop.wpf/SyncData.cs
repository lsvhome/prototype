using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Fex.Api;
using System.Runtime.Serialization;

namespace FexSync
{
    [DataContract]
    public class SyncData
    {
        [DataMember]
        public CommandBuildRemoteTree.CommandBuildRemoteTreeResponse Remote { get; set; }

        [DataMember]
        public CommandBuildLocalTree.CommandBuildLocalTreeResponse Local { get; set; }

        [DataMember]
        public IList<CommandBuildLocalTree.LocalFileMetaData> UploadList { get; set; } = new List<CommandBuildLocalTree.LocalFileMetaData>();

        [DataMember]
        public IList<CommandObjectView.CommandObjectViewResponseObject> DownloadList { get; set; } = new List<CommandObjectView.CommandObjectViewResponseObject>();
    }
}
