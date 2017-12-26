using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace FexSync.Data
{
    [Serializable]
    public class RemoteFile : RemoteFileBase
    {
        [Key]
        public int RemoteFileId { get; set; }

        public int? ParentRemoteFileId { get; set; }

        public int RemoteTreeId { get; set; }

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

        public string Name { get; set; }

        [NotMapped]
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

        public string Crc32 { get; set; }

        public ulong Size { get; set; }

        public int UploadTime { get; set; }
    }
}
