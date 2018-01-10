using System;

namespace FexSync.Data
{
    public abstract class RemoteFileBase
    {
        [Obsolete("Only for (De)Serialization purposes", true)]
        protected RemoteFileBase()
        {
        }

        protected RemoteFileBase(int uploadId, AccountSyncObject syncObject)
        {
            this.UploadId = uploadId;
            this.SyncObject = syncObject;
        }

        public int UploadId { get; set; }

        public AccountSyncObject SyncObject { get; set; }
    }
}
