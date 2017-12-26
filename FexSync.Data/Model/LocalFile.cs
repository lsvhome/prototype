using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace FexSync.Data
{
    [Serializable]
    public class LocalFile
    {
        [Key]
        public int LocalFileId { get; set; }

        [NotMapped]
        private string path;

        public string Path
        {
            get
            {
                return this.path;
            }

            set
            {
                this.path = value.Trim(System.IO.Path.DirectorySeparatorChar);
#if DEBUG
                if (System.IO.Path.IsPathRooted(this.path))
                {
                    throw new ApplicationException();
                }
#endif
            }
        }

        public ulong Length { get; set; }

        [NotMapped]
        public int LastWriteTimeUnix
        {
            get
            {
                return this.LastWriteTime.ToUnixTime();
            }

            set
            {
                this.LastWriteTime = value.FromUnixTime();
            }
        }

        public DateTime LastWriteTime { get; set; }

        //// public string Crc32 { get; set; }

        [NotMapped]
        [NonSerialized]
        private string sha1;

        public string Sha1
        {
            get
            {
                return this.sha1;
            }

            set
            {
                this.sha1 = value.ToLower();
            }
        }
    }
}
