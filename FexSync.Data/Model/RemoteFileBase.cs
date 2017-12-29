using System;

namespace FexSync.Data
{
    public abstract class RemoteFileBase
    {
        public int UploadId { get; set; }

        public AccountSyncObject SyncObject { get; set; }
    }
}
