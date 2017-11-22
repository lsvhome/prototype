using System;

namespace FexSync.Data
{
    public class RemoteFile: RemoteFileBase
    {
        public int RemoteFileId { get; set; }

        public string Name { get; set; }

        public string Sha1 { get; set; }

        public ulong Size { get; set; }

        public int UploadTime { get; set; }

    }
}
