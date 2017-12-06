using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FexSync.Data
{
    public class LocalFileModified
    {
        [Key]
        public int LocalFileModifiedId { get; set; }

        private string LocalFileOldXml { get; set; }

        [NotMapped]
        public LocalFile LocalFileOld
        {
            get
            {
                return Extenders.Parse<LocalFile>(this.LocalFileOldXml);
            }

            set
            {
                this.LocalFileOldXml = Extenders.Save<LocalFile>(value);
            }
        }

        private string LocalFileNewXml { get; set; }

        [NotMapped]
        public LocalFile LocalFileNew
        {
            get
            {
                return Extenders.Parse<LocalFile>(this.LocalFileNewXml);
            }

            set
            {
                this.LocalFileNewXml = Extenders.Save<LocalFile>(value);
            }
        }

        public string Path { get; set; }
    }
}
