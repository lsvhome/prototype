using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FexSync.Data
{
    public class RemoteFileModified
    {
        [Key]
        public int RemoteFileModifiedId { get; set; }

        private string RemoteFileOldXml { get; set; }

        [NotMapped]
        public RemoteFile RemoteFileOld
        {
            get
            {
                return Extenders.Parse<RemoteFile>(this.RemoteFileOldXml);
            }

            set
            {
                this.RemoteFileOldXml = Extenders.Save<RemoteFile>(value);
            }
        }

        private string RemoteFileNewXml { get; set; }

        [NotMapped]
        public RemoteFile RemoteFileNew
        {
            get
            {
                return Extenders.Parse<RemoteFile>(this.RemoteFileNewXml);
            }

            set
            {
                this.RemoteFileNewXml = Extenders.Save<RemoteFile>(value);
            }
        }

        public string Path { get; set; }
    }
}
