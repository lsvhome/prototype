using System;

namespace FexSync.Data
{
    public class LocalFile
    {
        public int LocalFileId { get; set; }

        //public string Name { get; set; }

        public string Path { get; set; }

        public long Length { get; set; }

        public DateTime LastWriteTime { get; set; }
        
        //public string Crc32 { get; set; }

        //public string Sha1 { get; set; }
    }
}
